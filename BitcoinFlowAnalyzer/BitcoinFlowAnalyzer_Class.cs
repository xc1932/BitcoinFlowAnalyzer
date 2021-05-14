using BitcoinBlockchain.Data;
using BitcoinUTXOSlicer;
using NBitcoin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using IDdatatype = System.Int32;

namespace BitcoinFlowAnalyzer
{
    public class BitcoinFlowAnalyzer_Class
    {
        public string sqlConnectionString = null;
        BitcoinUTXOSlicer_Class bitcoinUTXOSlicer;
        //染色池字典:初始化时使用一个或多个公司的地址集初始化，程序运行后添加被染色池地址染色的地址，最终保存染色池字典的结果
        //结果包括两张表，一张是Dic中的地址表，另一张是基因结果表，一个可疑地址可能有多个基因
        Dictionary<string, Address_Class> dyingPoolingDic = new Dictionary<string, Address_Class>();
        AddressParser_Class addressParser = new AddressParser_Class();
        HashSet<string> suspectAddressCluster;
        string suspectAddressStorePath=null;
        string DyingPoolingDicFilePath = null;
        string DyingPoolingDicFileName = null;

        public BitcoinFlowAnalyzer_Class() { }

        public BitcoinFlowAnalyzer_Class(string suspectAddressStorePath) 
        {
            //1.加载可疑地址
            Stopwatch timer1 = new Stopwatch();
            timer1.Start();
            suspectAddressCluster = loadSuspectAddressTodyingPoolingDic(suspectAddressStorePath);
            timer1.Stop();
            Console.WriteLine("可以地址数量:" + suspectAddressCluster.Count);
            Console.WriteLine("染色池字典地址数量:" + dyingPoolingDic.Count);
            Console.WriteLine("加载可疑地址用时:" + timer1.Elapsed);
        }

        public BitcoinFlowAnalyzer_Class(string suspectAddressStorePath, string blockchainFilePath, string blockProcessContextFilePath, string blockProcessContextFileName, 
            string UtxoSliceFilePath,string UtxoSliceFileName, string OpReturnFilePath, string AddressBalanceFilePath, string AddressBalanceFileName,string DyingPoolingDicFilePath,
            string DyingPoolingDicFileName,string sliceIntervalTimeType,int sliceIntervalTime, DateTime endTime, int endBlockHeight, string sqlConnectionString) 
        {                                    
            //1.初始化BitcoinUTXOSlicer_Class相关的参数                        
            bitcoinUTXOSlicer = new BitcoinUTXOSlicer_Class(blockchainFilePath, blockProcessContextFilePath, blockProcessContextFileName, UtxoSliceFilePath,
            UtxoSliceFileName, OpReturnFilePath, AddressBalanceFilePath, AddressBalanceFileName, sliceIntervalTimeType, sliceIntervalTime, endTime, endBlockHeight);
            //2.初始化BitcoinFlowAnalyzer相关的参数
            this.sqlConnectionString = sqlConnectionString;
            this.suspectAddressStorePath = suspectAddressStorePath;
            this.DyingPoolingDicFilePath = DyingPoolingDicFilePath;                 //DyingPoolingDic文件路径
            this.DyingPoolingDicFileName = DyingPoolingDicFileName;                 //DyingPoolingDic恢复文件名
            parameter_Detection();
            initialization_Database(true);
            //3.dyingPoolingDic加载
            if (DyingPoolingDicFileName==null)
            {
                //加载可疑地址
                Stopwatch timer1 = new Stopwatch();
                timer1.Start();
                suspectAddressCluster = loadSuspectAddressTodyingPoolingDic(suspectAddressStorePath);
                timer1.Stop();
                Console.WriteLine("可以地址数量:" + suspectAddressCluster.Count);
                Console.WriteLine("染色池字典地址数量:" + dyingPoolingDic.Count);
                Console.WriteLine("加载可疑地址用时:" + timer1.Elapsed);
            }
            else
            {
                //从中断处恢复
                restore_DyingPoolingDicContextForProgram();
            }
            
        }

        //I.染色地址加载和染色池字典初始化
        //1.获取聚类的可疑地址(单公司版)
        public HashSet<string> loadSuspectAddressTodyingPoolingDic(string path)
        {
            HashSet<string> suspectAddressCluster=null;
            using (StreamReader sr = File.OpenText(path))
            {
                JsonSerializer jsonSerializer = new JsonSerializer();
                suspectAddressCluster = jsonSerializer.Deserialize(sr, typeof(HashSet<string>)) as HashSet<string>;
            }
            if (suspectAddressCluster != null)
            {
                foreach (string suspectAddress in suspectAddressCluster)
                {
                    if (!dyingPoolingDic.ContainsKey(suspectAddress))
                    {
                        dyingPoolingDic.Add(suspectAddress, new Address_Class(true, DNA_Class.mtgoxDNAID));
                    }
                    else
                    {
                        Console.WriteLine("向染色池字典中重复添加了可疑地址!!!");
                    }
                    
                }
            }
            else
            {
                Console.WriteLine("可疑地址集合(suspectAddressCluster)为空!!!");
            }
            return suspectAddressCluster;
        }

        //II.交易追踪
        //1.判断opreturn输出
        public bool isOpreturn(TxOut txOut)
        {
            bool opreturnMark = false;
            int scriptLen = txOut.ScriptPubKey.ToBytes().Length;
            if (scriptLen >= 1)
            {
                if (txOut.ScriptPubKey.ToBytes()[0] == 0x6a)
                {
                    opreturnMark = true;
                }
                else
                {
                    if (scriptLen >= 2)
                    {
                        if (txOut.ScriptPubKey.ToBytes()[0] == 0x00 && txOut.ScriptPubKey.ToBytes()[1] == 0x6a)
                        {
                            opreturnMark = true;
                        }
                    }
                }
            }
            return opreturnMark;
        }        

        //2.脚本字符串转换为脚本数组        
        public byte[] scriptStrToByteArray(string scriptStr)
        {
            byte[] scriptByteArray = Org.BouncyCastle.Utilities.Encoders.Hex.Decode(scriptStr);
            return scriptByteArray;
        }        

        //3.判断铸币交易是否应该被追踪
        public bool coinbaseTxTraceJudging(Transaction transaction) 
        {
            foreach (TxOut transactionOutput in transaction.Outputs)
            {
                if (!isOpreturn(transactionOutput))
                {
                    bool isNonStandardPayment;
                    string outputAddress = addressParser.extractAddressFromScript(transactionOutput.ScriptPubKey.ToBytes(), 0x00, out isNonStandardPayment);
                    if (outputAddress != null)
                    {
                        if (dyingPoolingDic.ContainsKey(outputAddress))
                        {
                            return true;
                        }
                        else
                        {
                            //地址不在染色池字典中
                        }
                    }
                    else
                    {
                        //未提取出地址(可能提取函数有问题)
                    }
                }
                else
                {
                    //忽略opreturn
                }
            }
            return false;
        }

        //4.判断常规交易是否应该被追踪
        public bool regularTxTraceJudging(Transaction transaction) 
        {
            //1.判断输入中是否又可疑地址
            foreach (TxIn transactionInput in transaction.Inputs)
            {
                string sourceTxhashAndIndex = transactionInput.PrevOut.ToString();
                if (bitcoinUTXOSlicer.utxoDictionary.ContainsKey(sourceTxhashAndIndex))
                {
                    bool isNonStandardPayment;
                    byte[] scriptByteArray = scriptStrToByteArray(bitcoinUTXOSlicer.utxoDictionary[sourceTxhashAndIndex].script);                    
                    string sourceOutputAddress = addressParser.extractAddressFromScript(scriptByteArray, 0x00, out isNonStandardPayment);
                    if (sourceOutputAddress != null)
                    {
                        if (dyingPoolingDic.ContainsKey(sourceOutputAddress))
                        {
                            return true;
                        }
                        else
                        {
                            //地址不在染色池字典中
                        }
                    }
                    else
                    {
                        //未提取出地址(可能提取函数有问题)
                    }

                }
                else
                {
                    Console.WriteLine("当前交易中的输入不存在:" + sourceTxhashAndIndex);
                }
            }
            //2.判断输出中是否有可疑地址
            foreach (TxOut transactionOutput in transaction.Outputs)
            {
                if (!isOpreturn(transactionOutput))
                {
                    bool isNonStandardPayment;
                    string outputAddress = addressParser.extractAddressFromScript(transactionOutput.ScriptPubKey.ToBytes(), 0x00, out isNonStandardPayment);
                    if (outputAddress != null)
                    {
                        if (dyingPoolingDic.ContainsKey(outputAddress))
                        {
                            return true;
                        }
                        else
                        {
                            //地址不在染色池字典中
                        }
                    }
                    else
                    {
                        //未提取出地址(可能提取函数有问题)
                    }
                }
                else
                {
                    //忽略opreturn
                }
            }
            return false;
        }        

        //5.判断脚本对应地址是否为可疑地址
        public bool isSuspectedAddress(string scriptStr,out string suspectAddress)
        {
            bool isNonStandardPayment;
            byte[] scriptByteArray = scriptStrToByteArray(scriptStr);
            string outputAddress = addressParser.extractAddressFromScript(scriptByteArray, 0x00, out isNonStandardPayment);
            suspectAddress = outputAddress;
            if (outputAddress != null)
            {
                if (dyingPoolingDic.ContainsKey(outputAddress))
                {                    
                    return true;
                }
                else
                {
                    //地址不在染色池字典中
                }
            }
            else
            {
                //未提取出地址(可能提取函数有问题)
            }
            return false;
        }

        //6.铸币交易追踪
        //当输出中有可疑地址时才执行该函数
        public void traceCoinbaseTransaction(Transaction transaction)
        {
            foreach (TxOut txOut in transaction.Outputs)
            {
                string suspectAddress = null; 
                string script = new ByteArray(txOut.ScriptPubKey.ToBytes()).ToString();
                if (isSuspectedAddress(script,out suspectAddress))
                {
                    Address_Class suspectAddressObject = dyingPoolingDic[suspectAddress];
                    Coin_Class newCoin = new Coin_Class(txOut.Value.Satoshi);               //1.构造新币
                    if (suspectAddressObject.Isdyepool)                                     
                    {
                        newCoin.dyeGene(suspectAddressObject.DyeDNAID);                     //2.如果是染色地址，用地址对应的DNA染色
                    }
                    else
                    {
                        newCoin.dyeGene(DNA_Class.colorlessDNAID);                          //2.如果不是染色地址，用无色DNA染色
                    }
                    Coin_Class.mixCoin(newCoin, suspectAddressObject.BalanceCoin);          //3.将输出上的币和可疑地址的余额币融合
                    Coin_Class.mixCoin(newCoin, suspectAddressObject.TotalPassedCoin);      //4.将输出上的币和可疑地址的通过总币融合
                }
            }
        }

        //7.常规交易追踪
        //当前输入或输出中有可疑地址时才执行该函数
        public void traceRegularTransaction(Transaction transaction)
        {
            //1.融合输入上的币
            Coin_Class mixedInputsCoin = new Coin_Class(0);
            foreach (TxIn txIn in transaction.Inputs)
            {
                string sourceTxhashAndIndex = txIn.PrevOut.ToString();
                UTXOItem_Class utxoItem = bitcoinUTXOSlicer.utxoDictionary[sourceTxhashAndIndex];
                string suspectAddress = null;
                if (isSuspectedAddress(utxoItem.script, out suspectAddress))
                {
                    Address_Class suspectAddressObject = dyingPoolingDic[suspectAddress];
                    Coin_Class splitedCoin= Coin_Class.splitCoin(suspectAddressObject.BalanceCoin,utxoItem.value);
                    Coin_Class.mixCoin(splitedCoin,mixedInputsCoin);
                }
                else
                {
                    Coin_Class newCoin = new Coin_Class(utxoItem.value);
                    newCoin.dyeGene(DNA_Class.colorlessDNAID);
                    Coin_Class.mixCoin(newCoin, mixedInputsCoin);
                }
            }
            //2.分发输入融合的币到各个输出上
            if (mixedInputsCoin.GeneDictionary.Count==1&&mixedInputsCoin.GeneDictionary.ContainsKey(DNA_Class.colorlessDNAID))
            {
                //(1)无新可疑地址产生的分发
                foreach (TxOut txOut in transaction.Outputs)
                {
                    string suspectAddress = null;
                    string script = new ByteArray(txOut.ScriptPubKey.ToBytes()).ToString();
                    if (isSuspectedAddress(script, out suspectAddress))
                    {
                        Address_Class suspectAddressObject = dyingPoolingDic[suspectAddress];
                        Coin_Class newCoin = new Coin_Class(txOut.Value.Satoshi);               //1.构造新币
                        if (suspectAddressObject.Isdyepool)
                        {
                            newCoin.dyeGene(suspectAddressObject.DyeDNAID);                     //2.如果是染色地址，用地址对应的DNA染色
                        }
                        else
                        {
                            newCoin.dyeGene(DNA_Class.colorlessDNAID);                          //2.如果不是染色地址，用无色DNA染色
                        }
                        Coin_Class.mixCoin(newCoin, suspectAddressObject.BalanceCoin);          //3.将输出上的币和可疑地址的余额币融合
                        Coin_Class.mixCoin(newCoin, suspectAddressObject.TotalPassedCoin);      //4.将输出上的币和可疑地址的通过总币融合
                    }
                }
            }
            else
            {
                //(2)有新可疑地址产生的分发
                foreach (TxOut txOut in transaction.Outputs)
                {
                    string suspectAddress = null;
                    string script = new ByteArray(txOut.ScriptPubKey.ToBytes()).ToString();
                    Coin_Class splitedCoin = Coin_Class.splitCoin(mixedInputsCoin, txOut.Value.Satoshi);
                    if (isSuspectedAddress(script, out suspectAddress))
                    {
                        Address_Class suspectAddressObject = dyingPoolingDic[suspectAddress];                        
                        if (suspectAddressObject.Isdyepool)
                        {
                            splitedCoin.weightedDyeGene(suspectAddressObject.DyeDNAID, 0.5M);
                        }
                        //else
                        //{
                        //    splitedCoin.weightedDyeGene(DNA_Class.colorlessDNAID, 0.5M);
                        //}
                        Coin_Class.mixCoin(splitedCoin, suspectAddressObject.BalanceCoin);
                        Coin_Class.mixCoin(splitedCoin, suspectAddressObject.TotalPassedCoin);
                    }
                    else
                    {
                        //产生新的可疑地址(@@@@@@@@@@@@@@@@@@@@@@修改@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@)  
                        Coin_Class newCoin;
                        if (bitcoinUTXOSlicer.addressBalanceDic.ContainsKey(suspectAddress))
                        {
                            newCoin = new Coin_Class(bitcoinUTXOSlicer.addressBalanceDic[suspectAddress]);
                        }
                        else
                        {
                            newCoin = new Coin_Class(0);
                        }                        
                        newCoin.dyeGene(DNA_Class.colorlessDNAID);
                        Address_Class newSuspectedAddress = new Address_Class();//需要去UTXO中查找新可疑地址当前所拥有的余额币(可疑考虑在UTXOItem_Class中添加一个输出到的地址属性加快查找速度)
                        Coin_Class.mixCoin(newCoin,newSuspectedAddress.BalanceCoin);
                        Coin_Class.mixCoin(newCoin, newSuspectedAddress.TotalPassedCoin);

                        Coin_Class.mixCoin(splitedCoin, newSuspectedAddress.BalanceCoin);
                        Coin_Class.mixCoin(splitedCoin, newSuspectedAddress.TotalPassedCoin);
                        dyingPoolingDic.Add(suspectAddress, newSuspectedAddress);
                    }
                }
            }

        }

        //8.交易追踪
        public void traceTransaction(Transaction transaction)
        {
            if (transaction.IsCoinBase)
            {
                if (coinbaseTxTraceJudging(transaction))
                {
                    traceCoinbaseTransaction(transaction);
                }
            }
            else
            {
                if (regularTxTraceJudging(transaction))
                {
                    traceRegularTransaction(transaction);
                }
            }
        }

        //III.保存和恢复
        //1.保存dyingPoolingDic状态
        public void save_dyingPoolingDic(int processedBlockAmount, DateTime endBlockTime)
        { 
            string dyingPoolingDicFileFinalPath= Path.Combine(DyingPoolingDicFilePath, "DyingPoolingDic_" + processedBlockAmount + "_" + endBlockTime.ToString("yyyy年MM月dd日HH时mm分ss秒") + ".dat");
            using (StreamWriter sw = File.CreateText(dyingPoolingDicFileFinalPath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(sw, dyingPoolingDic);
            }
            bitcoinUTXOSlicer.orderedBlockchainParser.Compress(dyingPoolingDicFileFinalPath, true);
        }

        //2.恢复dyingPoolingDic上下文状态
        public void restore_DyingPoolingDicContextForProgram()
        {
            string dyingPoolingDicContextFileFinalPath = Path.Combine(DyingPoolingDicFilePath, DyingPoolingDicFileName);
            //判断给定文件名是压缩文件还是txt文件
            FileInfo fileName = new FileInfo(dyingPoolingDicContextFileFinalPath);
            if (fileName.Extension == ".rar")
            {
                Console.WriteLine("正在解压DyingPoolingDic上下文状态文件......");
                bitcoinUTXOSlicer.orderedBlockchainParser.Decompress(dyingPoolingDicContextFileFinalPath, false);
                dyingPoolingDicContextFileFinalPath = Path.Combine(DyingPoolingDicFilePath, Path.GetFileNameWithoutExtension(dyingPoolingDicContextFileFinalPath));
                if (File.Exists(dyingPoolingDicContextFileFinalPath))
                {
                    //1.反序列化DyingPoolingDic文件
                    Console.WriteLine("开始提取程序上下文状态文件数据(DyingPoolingDic).........");
                    Dictionary<string, Address_Class> dyingPoolingDicFileObject = null;
                    Stopwatch timer = new Stopwatch();
                    timer.Start();
                    try
                    {
                        using (StreamReader sr = File.OpenText(dyingPoolingDicContextFileFinalPath))
                        {
                            JsonSerializer jsonSerializer = new JsonSerializer();
                            dyingPoolingDicFileObject = jsonSerializer.Deserialize(sr, typeof(Dictionary<string, Address_Class>)) as Dictionary<string, Address_Class>;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine("DyingPoolingDic文件保存不完整或已经损坏。该错误可能是由于在保存DyingPoolingDic文件时提前终止程序造成的，或是人为修改了最近的DyingPoolingDic文件。");
                    }
                    timer.Stop();
                    Console.WriteLine("提取结束,反序列化切片用时:" + timer.Elapsed);
                    //恢复DyingPoolingDic
                    dyingPoolingDic = dyingPoolingDicFileObject;
                    File.Delete(dyingPoolingDicContextFileFinalPath);//删除解压后的文件DyingPoolingDic文件
                    Console.WriteLine("DyingPoolingDic上下文状态恢复成功.........");
                }
                else
                {
                    Console.WriteLine(dyingPoolingDicContextFileFinalPath + " 文件不存在!!!");
                }
            }
        }

        //3.参数检测
        public void parameter_Detection()
        {
            bool success = true;
            //1.检查suspectAddressStorePath和DyingPoolingDicFileName冲突
            if (suspectAddressStorePath != null && DyingPoolingDicFileName != null)
            {
                Console.WriteLine("初次加载可疑地址和从中断处恢复dyingPoolingDic两种模式只能选择一个!!!");
                success = false;
            }
            //2.DyingPoolingDic参数检查
            if (!Directory.Exists(DyingPoolingDicFilePath) && DyingPoolingDicFileName == null)
            {
                Directory.CreateDirectory(DyingPoolingDicFilePath);
            }
            if (!Directory.Exists(DyingPoolingDicFilePath) && DyingPoolingDicFileName != null)
            {
                Console.WriteLine(DyingPoolingDicFilePath + "不存在或错误!!!");
                success = false;
            }
            if (Directory.Exists(DyingPoolingDicFilePath) && DyingPoolingDicFileName != null)
            {
                string path = Path.Combine(DyingPoolingDicFilePath, DyingPoolingDicFileName);
                if (!File.Exists(path))
                {
                    Console.WriteLine(path + " 不存在!!!");
                    success = false;
                }
            }                        
        }

        //IV.追踪结果保存(写库saveToDatabase)
        //1.测试数据库连接状态
        public bool databaseConnectTest(bool printMark)
        {
            bool result = false;
            if (sqlConnectionString != null)
            {
                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
                {
                    try
                    {
                        sqlConnection.Open();
                        if (sqlConnection.State == ConnectionState.Open)
                        {
                            if (printMark)
                            {
                                Console.WriteLine("数据库连接成功!!!");
                            }
                            result = true;
                        }
                        else
                        {
                            if (printMark)
                            {
                                Console.WriteLine("数据库连接失败!!!");
                            }
                        }
                    }
                    catch
                    {
                        if (printMark)
                        {
                            Console.WriteLine("数据库连接失败!!!");
                        }
                    }
                    finally
                    {
                        sqlConnection.Close();
                    }
                }
            }
            else
            {
                if (printMark)
                {
                    Console.WriteLine("数据库连接字符串不能为空!!!");
                }
            }
            return result;
        }

        //2.判断数据库中是否存在某张表
        public bool tableExist(string tableName)
        {
            bool tableExist = false;
            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                try
                {
                    sqlConnection.Open();
                    using (DataTable dt = sqlConnection.GetSchema("Tables"))
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (string.Equals(tableName, dr[2].ToString()))
                            {
                                tableExist = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw e;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
            return tableExist;
        }

        //3.判断数据库中是否存在某个存储过程
        public bool procedureExist(string procedureName)
        {
            bool procedureExist = false;
            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                try
                {
                    sqlConnection.Open();
                    using (DataTable dt = sqlConnection.GetSchema("Procedures"))
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (string.Equals(procedureName, dr[2].ToString()))
                            {
                                procedureExist = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw e;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
            return procedureExist;
        }

        //4.判断数据库中是否存在某个表值
        public bool tableTypeExist(string tableTypeName)
        {
            bool tableTypeExist = false;
            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                try
                {
                    sqlConnection.Open();
                    using (DataTable dt = sqlConnection.GetSchema("DataTypes"))
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (string.Equals(tableTypeName, dr[0].ToString()))
                            {
                                tableTypeExist = true;
                                break;
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw e;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
            return tableTypeExist;
        }

        //5.创建交易追踪的相关结果表
        public void flowAnalyzerResultTableCreate(bool printMark)
        {
            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                sqlConnection.Open();
                SqlCommand sqlCommand;                               
                //1.创建可疑地址表
                if (!tableExist("SuspectAddress"))
                {
                    string commandStr = "CREATE TABLE [dbo].[SuspectAddress]([AddressID][bigint] NOT NULL PRIMARY KEY,[Address] [varchar](64) NOT NULL UNIQUE," +
                                        "[IsDyePool] [bit] NOT NULL,[DyeDNAID] [int] NOT NULL,[BalanceCoin] [int] NOT NULL,[TotalPassedCoin] [int] NOT NULL)";
                    sqlCommand = new SqlCommand(commandStr, sqlConnection);
                    sqlCommand.ExecuteNonQuery();
                    if (printMark)
                    {
                        Console.WriteLine("SuspectAddress表创建成功!!!");
                    }
                }
                else
                {
                    if (printMark)
                    {
                        Console.WriteLine("SuspectAddress表已存在!!!");
                    }
                }
                //2.创建基因结果表
                if (!tableExist("GeneResult"))
                {
                    string commandStr = "CREATE TABLE [dbo].[GeneResult]([GeneResultRecordID][bigint] NOT NULL PRIMARY KEY,[AddressID] [bigint] NOT NULL,[DNAID] [int] NOT NULL," +
                                        "[DNAAmountInBalance] [int] NOT NULL,[DNAWeightInBalance] [float] NOT NULL,[DNAAmountInTotal] [int] NOT NULL,[DNAWeightInTotal] [float] NOT NULL)";
                    sqlCommand = new SqlCommand(commandStr, sqlConnection);
                    sqlCommand.ExecuteNonQuery();
                    if (printMark)
                    {
                        Console.WriteLine("GeneResult表创建成功!!!");
                    }
                }
                else
                {
                    if (printMark)
                    {
                        Console.WriteLine("GeneResult表已存在!!!");
                    }
                }
                sqlConnection.Close();
            }


        }

        //6.创建交易追踪结果表的相关存储过程
        public void flowAnalyzerResultTableProcedureCreate(bool printMark)
        {
            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                sqlConnection.Open();
                SqlCommand sqlCommand;
                //1.创建SuspectAddress表的存储过程
                if (!procedureExist("Insert_SuspectAddress_Proc"))
                {                   
                    string commandStr = "CREATE PROC Insert_SuspectAddress_Proc @AddressID bigint,@Address varchar(64),@IsDyePool bit,@DyeDNAID int,@BalanceCoin int,@TotalPassedCoin int " +
                                        "AS BEGIN INSERT INTO[dbo].[SuspectAddress] VALUES (@AddressID,@Address,@IsDyePool,@DyeDNAID,@BalanceCoin,@TotalPassedCoin) END";
                    sqlCommand = new SqlCommand(commandStr, sqlConnection);
                    sqlCommand.ExecuteNonQuery();
                    if (printMark)
                    {
                        Console.WriteLine("存储过程Insert_SuspectAddress_Proc创建成功!!!");
                    }
                }
                else
                {
                    if (printMark)
                    {
                        Console.WriteLine("存储过程Insert_SuspectAddress_Proc已存在!!!");
                    }
                }
                //2.创建GeneResult表的存储过程
                if (!procedureExist("Insert_GeneResult_Proc"))
                {
                    string commandStr = "CREATE PROC Insert_GeneResult_Proc @GeneResultRecordID bigint,@AddressID bigint,@DNAID int,@DNAAmountInBalance int,@DNAWeightInBalance float ,DNAAmountInTotal int,DNAWeightInTotal float" +
                                        "AS BEGIN INSERT INTO[dbo].[GeneResult] VALUES (@GeneResultRecordID,@AddressID,@DNAID,@DNAAmountInBalance,@DNAWeightInBalance,@DNAAmountInTotal,@DNAWeightInTotal) END";
                    sqlCommand = new SqlCommand(commandStr, sqlConnection);
                    sqlCommand.ExecuteNonQuery();
                    if (printMark)
                    {
                        Console.WriteLine("存储过程Insert_GeneResult_Proc创建成功!!!");
                    }
                }
                else
                {
                    if (printMark)
                    {
                        Console.WriteLine("存储过程Insert_GeneResult_Proc已存在!!!");
                    }
                }                                                                                                                            
                sqlConnection.Close();
            }
        }

        //7.创建交易追踪结果表的相关表值
        public void flowAnalyzerResultTableTypeCreate(bool printMark)
        {
            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                sqlConnection.Open();
                SqlCommand sqlCommand;
                //1.创建SuspectAddressTableType表值
                if (!tableTypeExist("SuspectAddressTableType"))
                {
                    string commandStr = "CREATE TYPE SuspectAddressTableType AS TABLE([AddressID][bigint] NOT NULL,[Address] [varchar](64) NOT NULL," +
                                        "[IsDyePool] [bit] NOT NULL,[DyeDNAID] [int] NOT NULL,[BalanceCoin] [int] NOT NULL,[TotalPassedCoin] [int] NOT NULL)";
                    sqlCommand = new SqlCommand(commandStr, sqlConnection);
                    sqlCommand.ExecuteNonQuery();
                    if (printMark)
                    {
                        Console.WriteLine("SuspectAddressTableType表值创建成功!!!");
                    }
                }
                else
                {
                    if (printMark)
                    {
                        Console.WriteLine("SuspectAddressTableType表值已存在!!!");
                    }
                }
                //2.创建GeneResultTableType表值
                if (!tableTypeExist("GeneResultTableType"))
                {
                    string commandStr = "CREATE TYPE GeneResultTableType AS TABLE([GeneResultRecordID][bigint] NOT NULL,[AddressID] [bigint] NOT NULL,[DNAID] [int] NOT NULL," +
                                        "[DNAAmountInBalance] [int] NOT NULL,[DNAWeightInBalance] [float] NOT NULL,[DNAAmountInTotal] [int] NOT NULL,[DNAWeightInTotal] [float] NOT NULL)";
                    sqlCommand = new SqlCommand(commandStr, sqlConnection);
                    sqlCommand.ExecuteNonQuery();
                    if (printMark)
                    {
                        Console.WriteLine("GeneResultTableType表值创建成功!!!");
                    }
                }
                else
                {
                    if (printMark)
                    {
                        Console.WriteLine("GeneResultTableType表值已存在!!!");
                    }
                }                                                                
                sqlConnection.Close();
            }
        }

        //8.数据库初始化
        public bool initialization_Database(bool printMark)
        {
            bool successMark = true;
            if (databaseConnectTest(printMark))
            {
                try
                {
                    flowAnalyzerResultTableCreate(printMark);
                    flowAnalyzerResultTableProcedureCreate(printMark);
                    flowAnalyzerResultTableTypeCreate(printMark);
                }
                catch (Exception e)
                {
                    successMark = false;
                    Console.WriteLine(e.Message);
                    throw e;
                }
            }
            else
            {
                successMark = false;
            }
            return successMark;
        }

        //9.创建SuspectAddress的数据表值类型
        public DataTable get_SuspectAddress_TableSchema()
        {
            DataTable dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[] {
                new DataColumn("AddressID",typeof(Int64)),//0    
                new DataColumn("Address",typeof(String)),//1
                new DataColumn("IsDyePool",typeof(Boolean)),//2
                new DataColumn("DyeDNAID",typeof(Int32)),//3
                new DataColumn("BalanceCoin",typeof(Int32)),//4
                new DataColumn("TotalPassedCoin",typeof(Int32)),//5
            });
            return dt;
        }

        //10.创建GeneResult的数据表值类型
        public DataTable get_GeneResult_TableSchema()
        {
            DataTable dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[] {
                new DataColumn("GeneResultRecordID",typeof(Int64)),//0    
                new DataColumn("AddressID",typeof(Int64)),//1
                new DataColumn("DNAID",typeof(Int32)),//2
                new DataColumn("DNAAmountInBalance",typeof(Int32)),//3
                new DataColumn("DNAWeightInBalance",typeof(Double)),//4
                new DataColumn("DNAAmountInTotal",typeof(Int32)),//5
                new DataColumn("DNAWeightInTotal",typeof(Double)),//6
            });
            return dt;
        }

        //11.创建两张数据表值类型
        public void get_TwoTableSchema(out DataTable suspectAddressDataTable, out DataTable geneResultDataTable)
        {
            suspectAddressDataTable = get_SuspectAddress_TableSchema();
            geneResultDataTable = get_GeneResult_TableSchema();
        }

        //12.添加一条记录到SuspectAddress的数据表值类型
        public void addOneRowToSuspectAddressDataTable(DataTable dataTable,Int64 addressID,String address,
                                                       Boolean isDyePool,Int32 dyeDNAID,Int32 balanceCoin,Int32 totalPassedCoin)
        {
            DataRow dataRow = dataTable.NewRow();
            dataRow[0] = addressID;
            dataRow[1] = address;
            dataRow[2] = isDyePool;
            dataRow[3] = dyeDNAID;
            dataRow[4] = balanceCoin;
            dataRow[5] = totalPassedCoin;
            dataTable.Rows.Add(dataRow);
        }

        //13.添加一条记录到GeneResult的数据表值类型
        public void addOneRowToGeneResultDataTable(DataTable dataTable, Int64 geneResultRecordID, Int64 addressID, Int32 DNAID,
                                                   Int32 DNAAmountInBalance, Double DNAWeightInBalance, Int32 DNAAmountInTotal, Double DNAWeightInTotal)
        {
            DataRow dataRow = dataTable.NewRow();
            dataRow[0] = geneResultRecordID;
            dataRow[1] = addressID;
            dataRow[2] = DNAID;
            dataRow[3] = DNAAmountInBalance;
            dataRow[4] = DNAWeightInBalance;
            dataRow[5] = DNAAmountInTotal;
            dataRow[6] = DNAWeightInTotal;
            dataTable.Rows.Add(dataRow);
        }

        //14.将流分析结果写入到表值类型
        public void flowAnalyzerResultToDataTable(DataTable suspectAddressDataTable, DataTable geneResultDataTable)
        {
            Int64 addressID = 0;
            Int64 geneResultRecordID = 0;
            foreach (string suspectAddressStr in dyingPoolingDic.Keys)
            {
                Address_Class suspectAddressValueObject = dyingPoolingDic[suspectAddressStr];
                addOneRowToSuspectAddressDataTable(suspectAddressDataTable, addressID, suspectAddressStr, suspectAddressValueObject.isDyePool, suspectAddressValueObject.dyeDNAID,
                    (int)suspectAddressValueObject.balanceCoin.amount, (int)suspectAddressValueObject.totalPassedCoin.amount);
                foreach (IDdatatype DNAID in suspectAddressValueObject.balanceCoin.geneDictionary.Keys)
                {
                    Int32 DNAAmountInBalance = (int)suspectAddressValueObject.balanceCoin.geneDictionary[DNAID].amount;
                    Double DNAWeightInBalance = (double)suspectAddressValueObject.balanceCoin.geneDictionary[DNAID].percent;
                    Int32 DNAAmountInTotal = (int)suspectAddressValueObject.totalPassedCoin.geneDictionary[DNAID].amount;
                    Double DNAWeightInTotal = (double)suspectAddressValueObject.totalPassedCoin.geneDictionary[DNAID].percent;
                    addOneRowToGeneResultDataTable(geneResultDataTable, geneResultRecordID, addressID, DNAID, DNAAmountInBalance, DNAWeightInBalance, DNAAmountInTotal, DNAWeightInTotal);
                    geneResultRecordID++;
                }
                addressID++;
            }
        }

        //15.将SuspectAddress表值写入到数据库中
        public void suspectAddressTableValuedToDB(DataTable dataTable)
        {
            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                string TSqlStatement = "INSERT INTO[dbo].[SuspectAddress] (AddressID,Address,IsDyePool,DyeDNAID,BalanceCoin,TotalPassedCoin)" +
                                       "SELECT nc.AddressID,nc.Address,nc.IsDyePool,nc.DyeDNAID,nc.BalanceCoin,nc.TotalPassedCoin FROM @NewBulkTestTvp AS nc";
                SqlCommand cmd = new SqlCommand(TSqlStatement, sqlConnection);
                cmd.CommandTimeout = 0;
                SqlParameter catParam = cmd.Parameters.AddWithValue("@NewBulkTestTvp", dataTable);
                catParam.SqlDbType = SqlDbType.Structured;
                catParam.TypeName = "[dbo].[SuspectAddressTableType]";
                try
                {
                    sqlConnection.Open();
                    if (dataTable != null && dataTable.Rows.Count != 0)
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        //16.将GeneResult表值写入到数据库中
        public void geneResultTableValuedToDB(DataTable dataTable)
        {
            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionString))
            {
                string TSqlStatement = "INSERT INTO[dbo].[GeneResult] (GeneResultRecordID,AddressID,DNAID,DNAAmountInBalance,DNAWeightInBalance,DNAAmountInTotal,DNAWeightInTotal)" +
                                       "SELECT nc.GeneResultRecordID,nc.AddressID,nc.DNAID,nc.DNAAmountInBalance,nc.DNAWeightInBalance,nc.DNAAmountInTotal,nc.DNAWeightInTotal FROM @NewBulkTestTvp AS nc";
                SqlCommand cmd = new SqlCommand(TSqlStatement, sqlConnection);
                cmd.CommandTimeout = 0;
                SqlParameter catParam = cmd.Parameters.AddWithValue("@NewBulkTestTvp", dataTable);
                catParam.SqlDbType = SqlDbType.Structured;
                catParam.TypeName = "[dbo].[GeneResultTableType]";
                try
                {
                    sqlConnection.Open();
                    if (dataTable != null && dataTable.Rows.Count != 0)
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        //17.全部表值写入到数据库中
        public void allTableValueToDB(DataTable suspectAddressDataTable, DataTable geneResultDataTable)
        {
            suspectAddressTableValuedToDB(suspectAddressDataTable);
            geneResultTableValuedToDB(geneResultDataTable);
        }

        //18.分析结果写库
        public void saveToDatabase()
        {
            DataTable suspectAddressDataTable = get_SuspectAddress_TableSchema();
            DataTable geneResultDataTable = get_GeneResult_TableSchema();
            Console.WriteLine("正在将流分析结果添加到表值中......");
            Stopwatch sw1 = new Stopwatch();
            sw1.Start();
            flowAnalyzerResultToDataTable(suspectAddressDataTable, geneResultDataTable);
            sw1.Stop();
            Console.WriteLine("表值数据填充用时:"+sw1.Elapsed);
            Stopwatch sw2 = new Stopwatch();
            Console.WriteLine("正在将流分析结果写入到数据库......");
            sw2.Start();
            allTableValueToDB(suspectAddressDataTable, geneResultDataTable);
            sw2.Stop();
            Console.WriteLine("写入到数据库用时:"+sw2.Elapsed);
        }

        //V.运行
        public void run()
        {
            Console.WriteLine("***********************");
            Console.WriteLine("开始执行......");
            ParserBlock readyBlock;
            DateTime dateTime = DateTime.Now;
            while ((readyBlock=bitcoinUTXOSlicer.get_NextParserBlock())!=null) 
            {
                foreach (Transaction transaction in readyBlock.Transactions)
                {
                    //交易追踪
                    traceTransaction(transaction); 
                    //以交易为粒度更新UTXO
                    bitcoinUTXOSlicer.updateUTXO_ForOneTransaction(transaction);
                }
                //打印100个块处理用时
                if (bitcoinUTXOSlicer.orderedBlockchainParser.processedBlockAmount % 100 == 0)
                {
                    DateTime currentDateTime = DateTime.Now;
                    TimeSpan timeSpan = currentDateTime - dateTime;
                    Console.WriteLine("处理100个块用时:" + timeSpan);
                    dateTime = currentDateTime;
                }
                //常规打印
                if (bitcoinUTXOSlicer.orderedBlockchainParser.processedBlockAmount % 100 == 0)
                {
                    Console.WriteLine("已处理" + bitcoinUTXOSlicer.orderedBlockchainParser.processedBlockAmount + "个区块");
                    Console.WriteLine("当前区块时间:" + bitcoinUTXOSlicer.nextParserBlock.Header.BlockTime.DateTime);
                    Console.WriteLine("相同交易出现次数:" + bitcoinUTXOSlicer.sameTransactionCount);
                    Console.WriteLine("***********************");
                }
                //保存中间状态
                if(bitcoinUTXOSlicer.save_AllProgramContextFileWithoutDB())
                {
                    Console.WriteLine("正在保存第" + bitcoinUTXOSlicer.sliceFileAmount + "个dyingPoolingDic状态,请勿现在终止程序..........");
                    Stopwatch sw1 = new Stopwatch();
                    sw1.Start();
                    save_dyingPoolingDic(bitcoinUTXOSlicer.orderedBlockchainParser.processedBlockAmount,bitcoinUTXOSlicer.nextParserBlock.Header.BlockTime.DateTime);
                    sw1.Stop();
                    Console.WriteLine("保存第" + bitcoinUTXOSlicer.sliceFileAmount + "个dyingPoolingDic状态用时:" + sw1.Elapsed);
                }
                if (bitcoinUTXOSlicer.terminationConditionJudment())
                {
                    //追踪结果写库
                    saveToDatabase();
                    break;
                }
            }
        }

        public void JsonSerializerTest()
        {
            //默认只实例化类的公共成员
            Dictionary<string, Address_Class> dyingPoolingDicTest = new Dictionary<string, Address_Class>();
            Address_Class addressObj = new Address_Class(true, DNA_Class.mtgoxDNAID);

            dyingPoolingDicTest.Add("123", addressObj);
            string dyingPoolingDicFileFinalPath = Path.Combine(DyingPoolingDicFilePath, "DyingPoolingDicTest" + ".dat");
            using (StreamWriter sw = File.CreateText(dyingPoolingDicFileFinalPath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(sw, dyingPoolingDicTest);
            }
            Console.WriteLine("结束！！！！");
        }
    }
}
