using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using DataGather.DataTypes;
using DataGather.ErrorHandling;


namespace DataGather.IEXData
{
    public static class IEX
    {
        private const string m_baseURL = "https://cloud.iexapis.com/stable";
        private const string m_privateKey = "sk_960348b7b0894340908e58ce8fd73763";
        private const string m_publicKey = "pk_c27446c717b94c6ebddc3d452c435f03";


        // GetSymbols
        // Returns all symbols that IEX Cloud supports for intraday price updates
        public static async Task<List<Stock>> GetStocks()
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(m_baseURL + "/ref-data/symbols?token=" + m_publicKey);
            string result = await response.Content.ReadAsStringAsync();
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(result));

            // Have to set the appropriate settings for parsing the date correctly
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
            settings.DateTimeFormat = new DateTimeFormat("yyyy-MM-dd");
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<Stock>), settings);

            List<Stock> data = (List<Stock>)serializer.ReadObject(ms);

            return data;
        }

        // GetIntradayData
        // Returns all intraday data for a stock
        public static async Task<List<IntradayDataPoint>> GetIntradayData(string symbol)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.ConnectionClose = true; // Setting this to true avoids a rare http exception that can arise

            HttpResponseMessage response = null;
            try
            {
                response = await client.GetAsync(string.Format("{0}/stock/{1}/intraday-prices?token={2}&chartIEXOnly=true", m_baseURL, symbol, m_publicKey));
            }
            catch (HttpRequestException ex)
            {
                // Some times you get an "Error while copying content to a stream" exception, so just slow it down
                throw new IEXException(true);
            }

            if (response.StatusCode == (System.Net.HttpStatusCode)429)
            {
                // This exception means too many requests were recieved in a short period of time, so we need to slow down
                throw new IEXException(true);
            }
            else if (response.StatusCode == (System.Net.HttpStatusCode)451)
            {
                // This status is only returned when requesting MULG for some weird reason
                throw new IEXException();
            }
            
            string result = await response.Content.ReadAsStringAsync();

            // replace all instances of the string 'null' with '-1' because 'null' cannot be read into a float variable
            result = result.Replace("null", "-1");

            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(result));

            // Have to set the appropriate settings for parsing the date correctly
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
            settings.DateTimeFormat = new DateTimeFormat("yyyy-MM-dd");
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<IntradayDataPoint>), settings);

            List<IntradayDataPoint> data = null;
            try
            {
                data = (List<IntradayDataPoint>)serializer.ReadObject(ms);
            }
            catch (Exception)
            {
                throw new IEXException();
            }

            if (data == null || data.Count() == 0)
                return null;

            // Combine 'date' and 'minute' fields into a single DateTime field
            // Add symbol to the data as well
            foreach (IntradayDataPoint dp in data)
            {
                dp.symbol = symbol;

                dp.date = new DateTime(
                    dp.date.Year,
                    dp.date.Month,
                    dp.date.Day,
                    int.Parse(dp.minute.Substring(0, 2)),
                    int.Parse(dp.minute.Substring(3, 2)),
                    0);
            }

            FillIntradayGaps(data);

            return data;
        }

        private static void FillIntradayGaps(List<IntradayDataPoint> data)
        {
            // If the open for the first data point is -1, then we need to find the first non -1 value
            if (data[0].open == -1)
            {
                // find the first non -1 open value
                for (int iii = 1; iii < data.Count(); ++iii)
                {
                    if (data[iii].open != -1)
                    {
                        data[0].open = data[iii].open;
                        break;
                    }
                }

                // If there was no non -1 value, set the value to 0
                if (data[0].open == -1)
                    data[0].open = 0.0f;

                data[0].volume = 0;
            }

            // Iterate over all of the data
            for (int iii = 0; iii < data.Count(); ++iii)
            {
                // If the open is -1, set to previous day's close
                if (data[iii].open == -1)
                    data[iii].open = data[iii - 1].close;

                // If the close is -1, set it to the open
                if (data[iii].close == -1)
                    data[iii].close = data[iii].open;

                // If the high is -1, set to max(open, close)
                if (data[iii].high == -1)
                    data[iii].high = Math.Max(data[iii].open, data[iii].close);

                // If the low is -1, set to min(open, close)
                if (data[iii].low == -1)
                    data[iii].low = Math.Min(data[iii].open, data[iii].close);

                if (data[iii].volume == -1)
                    data[iii].volume = 0;

                if (data[iii].numberOfTrades == -1)
                    data[iii].numberOfTrades = 0;

                if (data[iii].notional == -1)
                    data[iii].notional = 0;

                if (data[iii].average == -1)
                    data[iii].average = (data[iii].open + data[iii].close) / 2.0f;
            }
        }


    }
}