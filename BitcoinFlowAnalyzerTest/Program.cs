using System;
using NBitcoin;
using BitcoinBlockchain.Data;
using OrderedBitcoinBlockchainParser;
using BitcoinFlowAnalyzer;
using BitcoinUTXOSlicer;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Text.Json;

namespace BitcoinFlowAnalyzerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ////1.初次运行
            //string suspectAddressStorePath = @"E:\Code\MyGithub\四个项目\AddressResultTXT";
            //string blockchainFilePath = @"F:\data\blocks";
            //string blockProcessContextFilePath = @"F:\BitcoinFlowAnalyzerResult\blockProcessContextFile";
            //string blockProcessContextFileName = null;
            //string UtxoSliceFilePath = @"F:\BitcoinFlowAnalyzerResult\sliceStateFile";
            //string UtxoSliceFileName = null;
            //string OpReturnFilePath = @"F:\BitcoinFlowAnalyzerResult\opreturnOutputFile";
            //string AddressBalanceFilePath = @"F:\BitcoinFlowAnalyzerResult\addressBalanceFile";
            //string AddressBalanceFileName = null;
            //string DyingPoolingDicFilePath = @"F:\BitcoinFlowAnalyzerResult\dyingPoolingDicFile";
            //string DyingPoolingDicFileName = null;
            //string sliceIntervalTimeType = Configuration_Class.Month;
            //int sliceIntervalTime = 1;
            //DateTime endTime = new DateTime(2010, 12, 30);
            //int endBlockHeight = 681572;
            //string sqlConnectionString = "Data Source=DESKTOP-0B83G22\\SQL2016;Initial Catalog=BitcoinUTXOSlice;Integrated Security=True";
            //BitcoinFlowAnalyzer_Class bitcoinFlowAnalyzer = new BitcoinFlowAnalyzer_Class(suspectAddressStorePath, blockchainFilePath, blockProcessContextFilePath, blockProcessContextFileName,
            //UtxoSliceFilePath, UtxoSliceFileName, OpReturnFilePath, AddressBalanceFilePath, AddressBalanceFileName, DyingPoolingDicFilePath, DyingPoolingDicFileName, sliceIntervalTimeType,
            //sliceIntervalTime, endTime, endBlockHeight, sqlConnectionString);
            //bitcoinFlowAnalyzer.run();

            //////2.增量启动
            //string suspectAddressStorePath = null;
            //string blockchainFilePath = @"F:\data\blocks";
            //string blockProcessContextFilePath = @"F:\BitcoinFlowAnalyzerResult\blockProcessContextFile";            
            //string UtxoSliceFilePath = @"F:\BitcoinFlowAnalyzerResult\sliceStateFile";            
            //string OpReturnFilePath = @"F:\BitcoinFlowAnalyzerResult\opreturnOutputFile";
            //string AddressBalanceFilePath = @"F:\BitcoinFlowAnalyzerResult\addressBalanceFile";            
            //string DyingPoolingDicFilePath = @"F:\BitcoinFlowAnalyzerResult\dyingPoolingDicFile";            
            //string AddressBalanceFileName = "AddressBalance_99246_2010年12月24日21时28分42秒.dat.rar";     //地址余额中断文件
            //string blockProcessContextFileName = "BPC_99246_2010年12月24日21时28分42秒.dat.rar";           //区块处理程序中断文件
            //string DyingPoolingDicFileName = "DyingPoolingDic_99246_2010年12月24日21时28分42秒.dat.rar";   //染色池中断文件
            //string UtxoSliceFileName = "UtxoSlice_99246_2010年12月24日21时28分42秒.dat.rar";               //切片程序中断文件
            //string sliceIntervalTimeType = Configuration_Class.Month;
            //int sliceIntervalTime = 1;
            //DateTime endTime = new DateTime(2015, 12, 30);
            //int endBlockHeight = 681572;
            //string sqlConnectionString = "Data Source=DESKTOP-0B83G22\\SQL2016;Initial Catalog=BitcoinUTXOSlice;Integrated Security=True";
            //BitcoinFlowAnalyzer_Class bitcoinFlowAnalyzer = new BitcoinFlowAnalyzer_Class(suspectAddressStorePath, blockchainFilePath, blockProcessContextFilePath, blockProcessContextFileName,
            //UtxoSliceFilePath, UtxoSliceFileName, OpReturnFilePath, AddressBalanceFilePath, AddressBalanceFileName, DyingPoolingDicFilePath, DyingPoolingDicFileName, sliceIntervalTimeType, sliceIntervalTime, endTime, endBlockHeight, sqlConnectionString);
            //bitcoinFlowAnalyzer.run();


            string MtGoxFilePath = @"F:\BitcoinFlowAnalyzerResult";
            BitcoinFlowAnalyzer_Class bfa = new BitcoinFlowAnalyzer_Class();
            bfa.extractMtGoxAddress(MtGoxFilePath);


            //BitcoinFlowAnalyzer_Class bfa = new BitcoinFlowAnalyzer_Class();
            ////HashSet<string> Bitfinex1 =bfa.loadSuspectAddressTodyingPoolingDic(@"E:\Code\MyGithub\四个项目\AddressResult\Bitfinex1.txt");
            //HashSet<string> totalAddressCluster = bfa.loadSuspectAddressTodyingPoolingDicFromMultiCompanies(@"E:\Code\MyGithub\四个项目\AddressResultTXT");
            //Console.WriteLine("地址总数:"+totalAddressCluster.Count);
            //Console.WriteLine("dyingPoolingDic:"+ bfa.dyingPoolingDic.Count);
            //Console.WriteLine(bfa.dyingPoolingDic["1EkiLq6bojYmgm83PiFmpjkoxsoummNNMp"].dyeDNAID);
            //Console.WriteLine(bfa.dyingPoolingDic["1BNq1SZJ5mAZVwFmumxeinsG9Ttw78kXRi"].dyeDNAID);

            //bitcoinFlowAnalyzer.JsonSerializerTest();


            //OrderedBitcoinBlockchainParser_Class orderedBitcoinBlockchainParser = new OrderedBitcoinBlockchainParser_Class(@"F:\data\blocks", @"F:\writedatabase\blockProcessContextFileForDatabase", null);
            //ParserBlock parserBlock = orderedBitcoinBlockchainParser.getNextBlock();
            //int len = parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes().Length;
            //Console.WriteLine(parserBlock.Transactions[0].Outputs[0].ScriptPubKey);
            //Console.WriteLine(parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToString());
            //string script = new ByteArray(parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes()).ToString();
            //Console.WriteLine("script:\t\t"+script);
            //byte[] byteArray = Org.BouncyCastle.Utilities.Encoders.Hex.Decode(script);
            //string script2 = new ByteArray(byteArray).ToString();
            //Console.WriteLine("script2:\t"+script2);
            //byte[] b1= parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes();
            //Console.WriteLine();
            //Console.WriteLine(parserBlock.Transactions[0].Outputs[0].ScriptPubKey.PaymentScript);
            //Console.WriteLine(parserBlock.Transactions[0].Outputs[0].ScriptPubKey.PaymentScript.ToString());
            //byte[] b2 = parserBlock.Transactions[0].Outputs[0].ScriptPubKey.PaymentScript.ToBytes();
            //Console.WriteLine();
            //Console.WriteLine(parserBlock.Transactions[0].Outputs[0].ScriptPubKey.PaymentScript.ToBytes().Length);
            //Console.WriteLine(parserBlock.Transactions[0].Outputs[0].ScriptPubKey.WitHash.ScriptPubKey);
            //Console.WriteLine("***********************");
            //Console.WriteLine(len);
            //Console.WriteLine("十六进制:{0:x}", parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes()[0]);
            //Console.WriteLine("十六进制:{0:x}", parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes()[1]);
            //Console.WriteLine("十六进制:{0:x}", parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes()[2]);
            //Console.WriteLine("十六进制:{0:x}", parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes()[len - 1]);
            //Console.WriteLine("************");
            //parserBlock = orderedBitcoinBlockchainParser.getNextBlock();
            //Console.WriteLine("十六进制:{0:x}", parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes()[0]);
            //Console.WriteLine("十六进制:{0:x}", parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes()[1]);
            //Console.WriteLine("十六进制:{0:x}", parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes()[2]);
            //Console.WriteLine("十六进制:{0:x}", parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes()[len - 1]);
            //Console.WriteLine("************");
            //parserBlock = orderedBitcoinBlockchainParser.getNextBlock();
            //Console.WriteLine("十六进制:{0:x}", parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes()[0]);
            //Console.WriteLine("十六进制:{0:x}", parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes()[1]);
            //Console.WriteLine("十六进制:{0:x}", parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes()[2]);
            //Console.WriteLine("十六进制:{0:x}", parserBlock.Transactions[0].Outputs[0].ScriptPubKey.ToBytes()[len - 1]);





            //AddressParser_Class addressParser = new AddressParser_Class();
            ////addressParser.extractAddressFromScript_Test();

            //Console.WriteLine("主网:" + Network.Main);
            //OrderedBitcoinBlockchainParser_Class orderedBlockchainParser = new OrderedBitcoinBlockchainParser_Class(@"F:\data\blocks", @"F:\Test", null);
            //ParserBlock readyBlock;
            //bool isNonStandardPayment;
            //int blockHeight = -1;
            //while ((readyBlock = orderedBlockchainParser.getNextBlock()) != null)
            //{
            //    blockHeight++;
            //    Console.WriteLine("区块" + blockHeight + ":");
            //    foreach (Transaction transaction in readyBlock.Transactions)
            //    {
            //        foreach (TxOut txOut in transaction.Outputs)
            //        {
            //            Console.WriteLine("******************");
            //            Console.WriteLine("脚本:" + txOut.ScriptPubKey.ToString());
            //            Console.WriteLine("-----------------------");
            //            //Console.WriteLine("脚本:" + txOut.ScriptPubKey.ToBytes().ToString());
            //            string addressStr = addressParser.extractAddressFromScript(txOut.ScriptPubKey.ToBytes(), 0x00, out isNonStandardPayment);
            //            Console.WriteLine("地址:" + addressStr);
            //            Console.WriteLine("******************");
            //        }
            //    }
            //    if (blockHeight == 5)
            //    {
            //        break;
            //    }
            //}
        }



    }
}
