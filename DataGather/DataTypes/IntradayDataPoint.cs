using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DataGather.DataTypes
{
    [DataContract]
    public class IntradayDataPoint
    {


        public string symbol { get; set; }


        [DataMember]
        public DateTime date { get; set; }
        [DataMember]
        public string minute { get; set; }
        [DataMember]
        public string label { get; set; }
        [DataMember]
        public float high { get; set; }
        [DataMember]
        public float low { get; set; }
        [DataMember]
        public float open { get; set; }
        [DataMember]
        public float close { get; set; }
        [DataMember]
        public float average { get; set; }
        [DataMember]
        public int volume { get; set; }
        [DataMember]
        public float notional { get; set; }
        [DataMember]
        public int numberOfTrades { get; set; }
    }

}
