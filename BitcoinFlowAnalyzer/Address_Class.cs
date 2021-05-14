using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using IDdatatype = System.Int32;

namespace BitcoinFlowAnalyzer
{
    public class Address_Class
    {
        public bool isDyePool = false;                             //该地址是否是染色池
        public IDdatatype dyeDNAID;                                //当地址是染色池时的染色DNAID 

        public Coin_Class balanceCoin = new Coin_Class(0);         //余额
        public Coin_Class totalPassedCoin = new Coin_Class(0);     //经过的币的总数
        //DateTime firstTransactionTime = DateTime.MaxValue;  //最早的交易时间
        //DateTime lastTransactionTime = DateTime.MinValue;   //最晚的交易时间

        internal bool Isdyepool { get => isDyePool; set => isDyePool = value; }
        internal IDdatatype DyeDNAID { get => dyeDNAID; set => dyeDNAID = value; }

        internal Coin_Class BalanceCoin { get => balanceCoin; }
        internal Coin_Class TotalPassedCoin { get => totalPassedCoin; }

        //非染色池地址构造(isDyePool = false,dyeDNAID=0)
        internal Address_Class() { }

        //染色池地址构造Addreaa_Class(true,dyeDNAID)
        internal Address_Class(bool isDyePool, IDdatatype dyeDNAID)
        {
            this.isDyePool = isDyePool;
            this.dyeDNAID = dyeDNAID;
        }



    }
}
