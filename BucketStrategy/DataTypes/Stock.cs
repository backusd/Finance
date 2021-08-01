using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BucketStrategy.DataTypes
{
    [DataContract]
    public class Stock
    {
        public Stock(string symbol, string name, DateTime date, string type, string iexId, string region, string currency, bool isEnabled, string figi, string cik)
        {
            this.symbol = symbol;
            this.name = name;
            this.date = date;
            this.type = type;
            this.iexId = iexId;
            this.region = region;
            this.currency = currency;
            this.isEnabled = isEnabled;
            this.figi = figi;
            this.cik = cik;
        }

        [DataMember]
        public string symbol { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public DateTime date { get; set; }
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public string iexId { get; set; }
        [DataMember]
        public string region { get; set; }
        [DataMember]
        public string currency { get; set; }
        [DataMember]
        public bool isEnabled { get; set; }
        [DataMember]
        public string figi { get; set; }
        [DataMember]
        public string cik { get; set; }
    }
}
