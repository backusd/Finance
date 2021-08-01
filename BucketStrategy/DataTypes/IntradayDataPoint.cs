using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BucketStrategy.DataTypes
{
    public class IntradayDataPoint : DataPoint
    {
        public float Average { get; }
        public float Notional { get; }
        public int NumberOfTrades { get; }

        public IntradayDataPoint(string symbol, DateTime datetime, float open, float high, float low, float close, float average, int volume, float notional, int numberOfTrades) :
            base(symbol, datetime, open, high, low, close, volume)
        {
            this.Average = average;
            this.Notional = notional;
            this.NumberOfTrades = numberOfTrades;
        }
    }

    [DataContract]
    public class _IntradayDataPoint
    {
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
