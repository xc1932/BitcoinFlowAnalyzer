using BitcoinBlockchain.Data;
using BitcoinUTXOSlicer;
using NBitcoin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

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

        public BitcoinFlowAnalyzer_Class(string suspectAddressStorePath, string blockchainFilePath, string blockProcessContextFilePath, string blockProcessContextFileName, string UtxoSliceFilePath,
            string UtxoSliceFileName, string OpReturnFilePath, string AddressBalanceFilePath, string AddressBalanceFileName, string sliceIntervalTimeType, int sliceIntervalTime, DateTime endTime, int endBlockHeight, string sqlConnectionString) 
        {
            //1.加载可疑地址
            Stopwatch timer1 = new Stopwatch();
            timer1.Start();
            suspectAddressCluster = loadSuspectAddressTodyingPoolingDic(suspectAddressStorePath);
            timer1.Stop();
            Console.WriteLine("可以地址数量:"+suspectAddressCluster.Count);
            Console.WriteLine("染色池字典地址数量:" + dyingPoolingDic.Count);
            Console.WriteLine("加载可疑地址用时:"+timer1.Elapsed);
            //2.初始化BitcoinUTXOSlicer_Class相关的参数
            this.sqlConnectionString = sqlConnectionString;
            //initialization_Database(true);
            bitcoinUTXOSlicer = new BitcoinUTXOSlicer_Class(blockchainFilePath, blockProcessContextFilePath, blockProcessContextFileName, UtxoSliceFilePath,
            UtxoSliceFileName, OpReturnFilePath, AddressBalanceFilePath, AddressBalanceFileName, sliceIntervalTimeType, sliceIntervalTime, endTime, endBlockHeight);
            //restore_DatabaseForBlockParserTable();
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
        //2.判断opreturn输出
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

        //脚本字符串转换为脚本数组        
        public byte[] scriptStrToByteArray(string scriptStr)
        {
            byte[] scriptByteArray = Org.BouncyCastle.Utilities.Encoders.Hex.Decode(scriptStr);
            return scriptByteArray;
        }        

        //判断铸币交易是否应该被追踪
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

        //判断常规交易是否应该被追踪
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

        //判断脚本对应地址是否为可疑地址
        public bool isSuspectedAddress(string scriptStr,out string suspectAddress)
        {
            bool isNonStandardPayment;
            byte[] scriptByteArray = scriptStrToByteArray(scriptStr);
            string outputAddress = addressParser.extractAddressFromScript(scriptByteArray, 0x00, out isNonStandardPayment);
            if (outputAddress != null)
            {
                if (dyingPoolingDic.ContainsKey(outputAddress))
                {
                    suspectAddress = outputAddress;
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
            suspectAddress = null;
            return false;
        }

        //铸币交易追踪
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

        //常规交易追踪
        //当前输入或输出中有可疑地址时才执行该函数
        public void traceRegularTransaction(Transaction transaction)
        {
            //融合输入上的币
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
            //分发输入融合的币到各个输出上
            if (mixedInputsCoin.GeneDictionary.Count==1&&mixedInputsCoin.GeneDictionary.ContainsKey(DNA_Class.colorlessDNAID))
            {
                //无新可疑地址产生的分发
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
                            newCoin.dyeGene(suspectAddressObject.DyeDNAID);        //2.如果是染色地址，用地址对应的DNA染色

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
                //有新可疑地址产生的分发
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
                        else
                        {
                            splitedCoin.dyeGene(DNA_Class.colorlessDNAID);
                        }
                        Coin_Class.mixCoin(splitedCoin, suspectAddressObject.BalanceCoin);
                        Coin_Class.mixCoin(splitedCoin, suspectAddressObject.TotalPassedCoin);
                    }
                    else
                    {
                        //产生新的可疑地址(@@@@@@@@@@@@@@@@@@@@@@修改@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@)                        
                        Address_Class newSuspectedAddress = new Address_Class();//需要去UTXO中查找新可疑地址当前所拥有的余额币(可疑考虑在UTXOItem_Class中添加一个输出到的地址属性加快查找速度)
                        Coin_Class.mixCoin(newSuspectedAddress.BalanceCoin,newSuspectedAddress.BalanceCoin);
                        Coin_Class.mixCoin(newSuspectedAddress.BalanceCoin, newSuspectedAddress.TotalPassedCoin);
                        dyingPoolingDic.Add(suspectAddress, newSuspectedAddress);
                    }
                }
            }

        }

        //交易追踪
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

        //III.追踪结果保存(写库saveToDatabase)

        public void run()
        {
            Console.WriteLine("开始执行......");
            ParserBlock readyBlock;

        } 
    }
}
