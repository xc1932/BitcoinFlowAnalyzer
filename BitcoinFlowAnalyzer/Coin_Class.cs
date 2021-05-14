using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using IDdatatype = System.Int32;

namespace BitcoinFlowAnalyzer
{
    public class Coin_Class
    {
        public decimal amount;
        public SortedDictionary<IDdatatype, DNA_Class> geneDictionary;

        internal decimal Amount { get => amount; set => amount = value; }
        internal SortedDictionary<IDdatatype, DNA_Class> GeneDictionary { get => geneDictionary; }

        //常规初始化一个币(构造地址时初始化的空币)
        internal Coin_Class(decimal amount)
        {
            this.amount = amount;
            geneDictionary = new SortedDictionary<IDdatatype, DNA_Class>(); //<DNA ID, DNA>
        }

        ////I.币的分割
        //1.构造一个分割出去的币(未调整每个基因的数量)
        internal Coin_Class(Coin_Class originalCoin, decimal splitedAmount)
        {
            amount = splitedAmount;
            geneDictionary = new SortedDictionary<IDdatatype, DNA_Class>(); //<DNA ID, DNA>
            foreach (DNA_Class originalDNA in originalCoin.geneDictionary.Values)
            {
                DNA_Class dna = new DNA_Class(originalDNA, splitedAmount);//DNA分割的时已经计算了每个基因的数量
                geneDictionary.Add(dna.DNAID, dna);
            }
        }

        //@@@2.币的分割操作，返回分割出去的币@@@
        //币的分割产生新的币，原来的币根据剩余数量判断是否存在，如果分割后还有剩余，则原来的币还有用
        internal static Coin_Class splitCoin(Coin_Class originalCoin, decimal splitedAmount)
        {
            if (splitedAmount > originalCoin.amount)
            {
                //processinfo_class.printinfo("error, splited amount is bigger than input amount.", true, true);
                Console.WriteLine("error, splited amount is bigger than input amount.");
            }
            Coin_Class splitedCoin = new Coin_Class(originalCoin, splitedAmount);
            originalCoin.amount -= splitedAmount;
            changeCoinDNAAmountByWeight(originalCoin);
            return (splitedCoin);
        }

        //3.根据原币中每个基因的比例调整分割后剩余币中每个基因的数量
        static void changeCoinDNAAmountByWeight(Coin_Class coin)
        {
            if (coin.amount < 0)
            {
                //processinfo_class.printinfo("erro, the amount of this coin < 0", true, true);
                Console.WriteLine("erro, the amount of this coin < 0");
            }
            if (coin.amount == 0)
            {
                coin.geneDictionary = new SortedDictionary<IDdatatype, DNA_Class>();//币分割后数量为0，基因清空
                return;
            }
            foreach (DNA_Class dna in coin.geneDictionary.Values)
            {
                dna.Amount = coin.amount * dna.Percent;
            }
        }

        ////II.币的融合
        //@@@1.币的融合操作，destinationCoin为融合后的新币@@@       
        internal static void mixCoin(Coin_Class originalCoin, Coin_Class destinationCoin,bool resetDestinationCoinGenes = true)
        {
            destinationCoin.amount += originalCoin.amount;
            foreach (DNA_Class originalDNA in originalCoin.geneDictionary.Values)
            {
                DNA_Class destinationDNA;
                if (destinationCoin.geneDictionary.TryGetValue(originalDNA.DNAID, out destinationDNA))
                {
                    destinationDNA.Amount += originalDNA.Amount;
                }
                else
                {
                    destinationDNA = new DNA_Class(originalDNA.DNAID, originalDNA.Amount);
                    destinationCoin.geneDictionary.Add(destinationDNA.DNAID, destinationDNA);
                }
            }
            if (resetDestinationCoinGenes)
            {
                resetGenes(destinationCoin);
            }
        }

        //2.重新调整基因的比例
        internal static void resetGenes(Coin_Class coin)
        {
            if (coin.amount > 0)
            {
                decimal dnaPercentSum = 0;
                foreach (DNA_Class dna in coin.geneDictionary.Values)
                {
                    dna.Percent = dna.Amount / coin.amount;
                    dnaPercentSum += dna.Percent;
                }
                if (dnaPercentSum - 1 > 0.01m || 1 - dnaPercentSum > 0.01m)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("it seems the error is a little big. the sum of DNA percentage is {0}", dnaPercentSum);
                    //processinfo_class.printinfo(sb.ToString(), true, true);
                    Console.WriteLine("It seems the error is a little big. the sum of DNA percentage is {0}", dnaPercentSum);
                }
            }
        }

        ////III.复合染色
        //@@@1.第一次被染色@@@
        internal void dyeGene(IDdatatype dnaID)
        {
            geneDictionary = new SortedDictionary<IDdatatype, DNA_Class>();
            geneDictionary.Add(dnaID, new DNA_Class(dnaID, amount, 1));
        }

        //@@@2.多次染色(复合基因染色)@@@
        internal void weightedDyeGene(IDdatatype dnaID, decimal weight)
        {
            if (weight < 0 || weight > 1)
            {
                string s = string.Format("error, weight={0}. weight should be 0<=weight<=1.", weight);
                //processinfo_class.printinfo(s, true, true);
                Console.WriteLine("Error, weight={0}. weight should be 0<=weight<=1.", weight);
            }
            Coin_Class coin2 = splitCoin(this, Amount * weight);//按权重分割出去的基因为G2的币                                 (1)
            coin2.dyeGene(dnaID);                               //将基因为G2的币用新的DNA染色                                  (2)
            mixCoin(coin2, this);                               //将按权重分割后剩余的基因为G1的币和重新染色的基因为G2的币融合 (3)
        }

        ////IV.获取当前币中某个基因的百分比
        internal decimal getPercentOfTargetDNA(IDdatatype targetDNAID)
        {
            decimal percentOfTargetDNA = 0;
            DNA_Class dna;
            if (geneDictionary.TryGetValue(targetDNAID, out dna))
            {
                percentOfTargetDNA = dna.Percent;
            }
            return (percentOfTargetDNA);
        }
    }
}
