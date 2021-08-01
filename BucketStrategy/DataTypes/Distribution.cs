using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BucketStrategy.DataBase;

namespace BucketStrategy.DataTypes
{
    public enum TimeUnit
    {
        MINUTE,
        DAY
    }

    public enum NormalizationMethod
    {
        DIVIDE_BY_MAX,
        SLOPES
    }

    public class OHLCOptions
    {
        public OHLCOptions()
        {
            Open = false;
            High = false;
            Low = false;
            Close = false;
        }
        public bool Open { get; set; }
        public bool High { get; set; }
        public bool Low { get; set; }
        public bool Close { get; set; }
    }

    public class Distribution
    {
        private string m_name;
        private List<Stock> m_stocks;
        private TimeUnit m_timeUnit;
        private OHLCOptions m_ohlcOptions;
        private DateTime m_minDate;
        private DateTime m_maxDate;
        private int m_windowSize;
        private int m_returnRange;
        private int m_numberOfBuckets;
        private NormalizationMethod m_normalizationMethod;

        private List<Bucket> m_buckets;

        public Distribution(string name, List<Stock> stocks, TimeUnit timeUnit, OHLCOptions ohlcOptions, 
                            DateTime minDate, DateTime maxDate, int windowSize, int returnRange, 
                            int numberOfBuckets, NormalizationMethod normalizationMethod)
        {
            // Don't throw any exceptions here - wait to do checking in the CreateBuckets method
            m_name = name;
            m_stocks = stocks;
            m_timeUnit = timeUnit;
            m_ohlcOptions = ohlcOptions;
            m_minDate = minDate;
            m_maxDate = maxDate;
            m_windowSize = windowSize;
            m_returnRange = returnRange;
            m_numberOfBuckets = numberOfBuckets;
            m_normalizationMethod = normalizationMethod;

            m_buckets = null;
        }

        public void CreateBuckets()
        {
            ValidateInput();

            m_buckets = new List<Bucket>();

            InitialAllocation();

            Redistribute();
        }

        private void ValidateInput()
        {
            // Make sure name distribution name does not already exist
            if (DB.DistributionExists(m_name))
                throw new Exception(string.Format("Distribution name '{0}' already exists", m_name));
            
            // For each stock, make sure stock has data covering min/max range
            foreach (Stock stock in m_stocks)
            {
                Tuple<DateTime, DateTime> dates = null;
                try
                {
                    dates = DB.GetStockMinMaxDates(stock, m_timeUnit);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Caught Exception: {0}", ex.Message));
                }

                if (dates == null)
                    throw new Exception(string.Format("Min/max dates not returned for stock '{0}'", stock.symbol));

                if (m_minDate < dates.Item1)
                    throw new Exception(string.Format("MinDate {0} is outside range for stock {1}: {2} - {3}", m_minDate.ToString("yyyy-MM-dd HH:mm:ss"), stock.symbol, dates.Item1.ToString("yyyy-MM-dd HH:mm:ss"), dates.Item2.ToString("yyyy-MM-dd HH:mm:ss")));

                if (m_maxDate > dates.Item2)
                    throw new Exception(string.Format("MaxDate {0} is outside range for stock {1}: {2} - {3}", m_minDate.ToString("yyyy-MM-dd HH:mm:ss"), stock.symbol, dates.Item1.ToString("yyyy-MM-dd HH:mm:ss"), dates.Item2.ToString("yyyy-MM-dd HH:mm:ss")));
            }

            // To make sure the window size / return range / number of buckets is valid, gather data for the first stock
            if (m_timeUnit == TimeUnit.MINUTE)
            {
                List<IntradayDataPoint> data = DB.GetIntradayData(m_stocks[0], m_minDate, m_maxDate);

                if (data.Count < m_windowSize + m_returnRange + m_numberOfBuckets)
                    throw new Exception(string.Format("Window size + return range + number of buckets '{0} + {1} + {2}' too large for amount data returned: {3}", m_windowSize, m_returnRange, m_numberOfBuckets, data.Count));
            }
            else if (m_timeUnit == TimeUnit.DAY)
            {
                /*
                List<DailyDataPoint> data = DB.GetDailyData(m_stocks[0], m_minDate, m_maxDate);

                if (data.Count < m_windowSize + m_returnRange + m_numberOfBuckets)
                    throw new Exception(string.Format("Window size + return range + number of buckets '{0} + {1} + {2}' too large for amount data returned: {3}", m_windowSize, m_returnRange, m_numberOfBuckets, data.Count));
            */
            }

        }

        private void InitialAllocation()
        {
            // List to hold all windows
            List<Window> windows = new List<Window>();

            // Gather data for each stock
            foreach (Stock stock in m_stocks)
            {
                List<IntradayDataPoint> data = DB.GetIntradayData(stock, m_minDate, m_maxDate);

                // Iterate over data creating each window
                for (int iii = 0; iii < data.Count; ++iii)
                {
                    windows.Add(new Window(
                        data.GetRange(iii, m_windowSize).ToList<DataPoint>(), 
                        data.GetRange(iii + m_windowSize, m_returnRange).ToList<DataPoint>(), 
                        m_ohlcOptions,
                        m_normalizationMethod
                    ));
                }
            }

            // Allocate windows into buckets
            float bestSimilarity;
            Bucket bestBucket = null;
            float threshold = 0.0f;
            foreach (Window window in windows)
            {
                if (m_buckets.Count == 0)
                {
                    m_buckets.Add(new Bucket());
                    m_buckets[0].AddWindow(window);
                    continue;
                }

                bestSimilarity = 0.0f;
                bestBucket = null;

                // Iterate over each bucket finding the best similarity
                foreach (Bucket bucket in m_buckets)
                {
                    float similarity = bucket.ComputeSimilarity(window, m_ohlcOptions);

                    if (similarity > threshold)
                    {
                        bestSimilarity = similarity;
                        bestBucket = bucket;
                    }    
                }

                // No match, create new bucket
                if (bestBucket == null)
                {
                    bestBucket = new Bucket();
                    bestBucket.AddWindow(window);
                    m_buckets.Add(bestBucket);
                }
            }
        }

        private void Redistribute()
        {

        }
    }
}
