using System;
using NBitcoin;
using BitcoinBlockchain.Data;
using OrderedBitcoinBlockchainParser;
using BitcoinFlowAnalyzer;

namespace BitcoinFlowAnalyzerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            AddressParser_Class addressParser = new AddressParser_Class();
            //addressParser.extractAddressFromScript_Test();

            Console.WriteLine("主网:" + Network.Main);
            OrderedBitcoinBlockchainParser_Class orderedBlockchainParser = new OrderedBitcoinBlockchainParser_Class(@"F:\data\blocks", @"F:\Test", null);
            ParserBlock readyBlock;
            bool isNonStandardPayment;
            int blockHeight = -1;
            while ((readyBlock = orderedBlockchainParser.getNextBlock()) != null)
            {
                blockHeight++;
                Console.WriteLine("区块" + blockHeight + ":");
                foreach (Transaction transaction in readyBlock.Transactions)
                {
                    foreach (TxOut txOut in transaction.Outputs)
                    {
                        Console.WriteLine("******************");
                        Console.WriteLine("脚本:" + txOut.ScriptPubKey.ToString());
                        Console.WriteLine("-----------------------");
                        //Console.WriteLine("脚本:" + txOut.ScriptPubKey.ToBytes().ToString());
                        string addressStr = addressParser.extractAddressFromScript(txOut.ScriptPubKey.ToBytes(), 0x00, out isNonStandardPayment);
                        Console.WriteLine("地址:" + addressStr);
                        Console.WriteLine("******************");
                    }
                }
                if (blockHeight == 5)
                {
                    break;
                }
            }
        }
    }
}
