using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BucketStrategy.DataTypes
{
    public class DataPoint
    {
        public DataPoint(string symbol, DateTime dateTime, float open, float high, float low, float close, int volume)
        {
            this.Symbol = symbol;
            this.DateTime = dateTime;
            this.Open = open;
            this.High = high;
            this.Low = low;
            this.Close = close;
            this.Volume = volume;
        }

        public string Symbol { get; }
        public DateTime DateTime { get; }
        public float Open { get; }
        public float High { get; }
        public float Low { get; }
        public float Close { get; }
        public int Volume { get; }
        

    }
}
