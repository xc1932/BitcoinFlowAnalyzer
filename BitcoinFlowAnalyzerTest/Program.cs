using System;
using NBitcoin;
using BitcoinBlockchain.Data;
using OrderedBitcoinBlockchainParser;
using BitcoinFlowAnalyzer;
using BitcoinUTXOSlicer;
using System.Collections.Generic;


namespace BitcoinFlowAnalyzerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ////1.初次运行
            string suspectAddressStorePath = @"E:\Code\BlockChainProject\workspace\AddressClusterFile\AddressClusterFinalResult.txt";
            string blockchainFilePath = @"F:\data\blocks";
            string blockProcessContextFilePath = @"F:\BitcoinFlowAnalyzerResult\blockProcessContextFile";
            string blockProcessContextFileName = null;
            string UtxoSliceFilePath = @"F:\BitcoinFlowAnalyzerResult\sliceStateFile";
            string UtxoSliceFileName = null;
            string OpReturnFilePath = @"F:\BitcoinFlowAnalyzerResult\opreturnOutputFile";
            string AddressBalanceFilePath = @"F:\BitcoinFlowAnalyzerResult\addressBalanceFile";
            string AddressBalanceFileName = null;
            string DyingPoolingDicFilePath = @"F:\BitcoinFlowAnalyzerResult\dyingPoolingDicFile";
            string DyingPoolingDicFileName = null;
            string sliceIntervalTimeType = Configuration_Class.Month;
            int sliceIntervalTime = 1;
            DateTime endTime = new DateTime(2009, 2, 15);
            int endBlockHeight = 681572;
            string sqlConnectionString = "Data Source=DESKTOP-0B83G22\\SQL2016;Initial Catalog=BitcoinUTXOSlice;Integrated Security=True";
            BitcoinFlowAnalyzer_Class bitcoinFlowAnalyzer = new BitcoinFlowAnalyzer_Class(suspectAddressStorePath, blockchainFilePath, blockProcessContextFilePath, blockProcessContextFileName,
            UtxoSliceFilePath, UtxoSliceFileName, OpReturnFilePath, AddressBalanceFilePath, AddressBalanceFileName, DyingPoolingDicFilePath, DyingPoolingDicFileName, sliceIntervalTimeType,
            sliceIntervalTime, endTime, endBlockHeight, sqlConnectionString);
            bitcoinFlowAnalyzer.run();

            //////2.增量启动
            //string suspectAddressStorePath = null;
            //string blockchainFilePath = @"F:\data\blocks";
            //string blockProcessContextFilePath = @"F:\BitcoinFlowAnalyzerResult\blockProcessContextFile";
            //string blockProcessContextFileName = "BPC_2755_2009年02月02日18时21分36秒.dat.rar";           //
            //string UtxoSliceFilePath = @"F:\BitcoinFlowAnalyzerResult\sliceStateFile";
            //string UtxoSliceFileName = "UtxoSlice_2755_2009年02月02日18时21分36秒.dat.rar";               //
            //string OpReturnFilePath = @"F:\BitcoinFlowAnalyzerResult\opreturnOutputFile";
            //string AddressBalanceFilePath = @"F:\BitcoinFlowAnalyzerResult\addressBalanceFile";
            //string AddressBalanceFileName = "AddressBalance_2755_2009年02月02日18时21分36秒.dat.rar";     //
            //string DyingPoolingDicFilePath = @"F:\BitcoinFlowAnalyzerResult\dyingPoolingDicFile";
            //string DyingPoolingDicFileName = "DyingPoolingDic_2755_2009年02月02日18时21分36秒.dat.rar";   //
            //string sliceIntervalTimeType = Configuration_Class.Month;
            //int sliceIntervalTime = 1;
            //DateTime endTime = new DateTime(2021, 12, 12);
            //int endBlockHeight = 681572;
            //string sqlConnectionString = "Data Source=DESKTOP-0B83G22\\SQL2016;Initial Catalog=BitcoinUTXOSlice;Integrated Security=True";
            //BitcoinFlowAnalyzer_Class bitcoinFlowAnalyzer = new BitcoinFlowAnalyzer_Class(suspectAddressStorePath, blockchainFilePath, blockProcessContextFilePath, blockProcessContextFileName,
            //UtxoSliceFilePath, UtxoSliceFileName, OpReturnFilePath, AddressBalanceFilePath, AddressBalanceFileName, DyingPoolingDicFilePath, DyingPoolingDicFileName, sliceIntervalTimeType, sliceIntervalTime, endTime, endBlockHeight, sqlConnectionString);
            //bitcoinFlowAnalyzer.run();


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
