using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using IDdatatype = System.Int32;

namespace BitcoinFlowAnalyzer
{
    public class DNA_Class
    {
        public IDdatatype dnaID;
        public decimal amount;
        public decimal percent;

        internal IDdatatype DNAID { get => dnaID; }
        internal decimal Amount { get => amount; set => amount = value; }
        internal decimal Percent { get => percent; set => percent = value; }
        internal const IDdatatype colorlessDNAID = 0;
        //internal const IDdatatype coinbaseDNAID = 0;
        internal const IDdatatype mtgoxDNAID = 1;

        internal DNA_Class(){}

        //未给出percent属性的构造
        internal DNA_Class(IDdatatype dnaID, decimal amount)
        {
            this.dnaID = dnaID;
            this.amount = amount;
        }

        //常规初始化DNA
        internal DNA_Class(IDdatatype dnaID, decimal amount, decimal percent)
        {
            this.dnaID = dnaID;
            this.amount = amount;
            this.percent = percent;
        }

        //币的分割:创建一个分割出去的币，分割币时仅是质量的分割，DNA成分和比例不变
        internal DNA_Class(DNA_Class originalDNA, decimal splitedAmount)
        {
            dnaID = originalDNA.dnaID;
            percent = originalDNA.percent;
            amount = splitedAmount * percent;
        }
    }
}
