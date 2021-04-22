using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
//using System.Data.SqlClient;
using IDdatatype = System.Int32;

namespace BitcoinFlowAnalyzer
{
    public class Address_Class
    {
        IDdatatype addressID;
        bool isDyePool = false;                             //该地址是否是染色池
        IDdatatype dyeDNAID;                                //当地址是染色池时的染色DNAID 
        bool newClustered = false;                          //是否被重新聚类
        IDdatatype clusterID = -1;                          //聚类ID


        Coin_Class balanceCoin = new Coin_Class(0);         //余额
        Coin_Class totalPassedCoin = new Coin_Class(0);     //经过的币的总数
        DateTime firstTransactionTime = DateTime.MaxValue;  //最早的交易时间
        DateTime lastTransactionTime = DateTime.MinValue;   //最晚的交易时间

        internal IDdatatype AddressID { get => addressID; }
        internal bool Isdyepool { get => isDyePool; set => isDyePool = value; }
        internal IDdatatype DyeDNAID { get => dyeDNAID; set => dyeDNAID = value; }
        internal bool NewClustered { get => newClustered; set => newClustered = value; }
        internal IDdatatype ClusterID { get => clusterID; set => clusterID = value; }

        internal Coin_Class Balancecoin { get => balanceCoin; }
        internal Coin_Class Totalpassedcoin { get => totalPassedCoin; }

        internal Address_Class(IDdatatype addressID)
        {
            this.addressID = addressID;
        }



    }
}
