using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BucketStrategy.DataTypes
{
    public class Window
    {
        private List<DataPoint> m_windowData;
        private List<DataPoint> m_returnData;


        public Window(Window window)
        {
            m_windowData = window.m_windowData.ToList();
            m_returnData = window.m_returnData.ToList();
        }
        public Window(List<Window> windows)
        {
            // When passed a list of windows, we need to compute the aggregate of all windows

            m_windowData = new List<DataPoint>();
            m_returnData = new List<DataPoint>();

            // Iterate over each data point in the window
            float oSum, hSum, lSum, cSum;
            int vSum;
            for (int iii = 0; iii < windows[0].m_windowData.Count; ++iii)
            {
                oSum = 0.0f;
                hSum = 0.0f;
                lSum = 0.0f;
                cSum = 0.0f;
                vSum = 0;

                // Iterate over each window
                for (int jjj = 0; jjj < windows.Count; ++jjj)
                {
                    oSum += windows[jjj].m_windowData[iii].Open;
                    hSum += windows[jjj].m_windowData[iii].High;
                    lSum += windows[jjj].m_windowData[iii].Low;
                    cSum += windows[jjj].m_windowData[iii].Close;
                    vSum += windows[jjj].m_windowData[iii].Volume;
                }

                m_windowData.Add(new DataPoint(
                    "",
                    new DateTime(),
                    oSum / windows.Count,
                    hSum / windows.Count,
                    lSum / windows.Count,
                    cSum / windows.Count,
                    vSum / windows.Count
                ));
            }

            // Iterate over each data point in the return range
            for (int iii = 0; iii < windows[0].m_returnData.Count; ++iii)
            {
                oSum = 0.0f;
                hSum = 0.0f;
                lSum = 0.0f;
                cSum = 0.0f;
                vSum = 0;

                // Iterate over each window
                for (int jjj = 0; jjj < windows.Count; ++jjj)
                {
                    oSum += windows[jjj].m_returnData[iii].Open;
                    hSum += windows[jjj].m_returnData[iii].High;
                    lSum += windows[jjj].m_returnData[iii].Low;
                    cSum += windows[jjj].m_returnData[iii].Close;
                    vSum += windows[jjj].m_returnData[iii].Volume;
                }

                m_returnData.Add(new DataPoint(
                    "",
                    new DateTime(),
                    oSum / windows.Count,
                    hSum / windows.Count,
                    lSum / windows.Count,
                    cSum / windows.Count,
                    vSum / windows.Count
                ));
            }
        }
        public Window(List<DataPoint> windowData, List<DataPoint> returnData, OHLCOptions ohlcOptions, NormalizationMethod normalizationMethod)
        {
            m_windowData = new List<DataPoint>();
            m_returnData = new List<DataPoint>();

            // get list of floats based off of ohlc options
            List<float> data = new List<float>();
            foreach (DataPoint dp in windowData)
            {
                if (ohlcOptions.Open)  data.Add(dp.Open);
                if (ohlcOptions.High)  data.Add(dp.High);
                if (ohlcOptions.Low)   data.Add(dp.Low);
                if (ohlcOptions.Close) data.Add(dp.Close);
            }

            if (normalizationMethod == NormalizationMethod.DIVIDE_BY_MAX)
            {
                float max = data.Max();

                foreach (DataPoint dp in windowData)
                {
                    m_windowData.Add(new DataPoint(
                        dp.Symbol,
                        dp.DateTime,
                        dp.Open / max,
                        dp.High / max,
                        dp.Low / max,
                        dp.Close / max,
                        dp.Volume
                    ));
                }

                foreach (DataPoint dp in returnData)
                {
                    m_returnData.Add(new DataPoint(
                        dp.Symbol,
                        dp.DateTime,
                        dp.Open / max,
                        dp.High / max,
                        dp.Low / max,
                        dp.Close / max,
                        dp.Volume
                    ));
                }
            }
            
        }
    }
}
