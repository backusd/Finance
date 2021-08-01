using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BucketStrategy.DataTypes
{
    public class Bucket
    {
        private List<Window> m_windows;
        private Window m_aggregate;

        public Bucket()
        {
            m_windows = new List<Window>();
            m_aggregate = null;
        }

        public void AddWindow(Window window)
        {
            m_windows.Add(window);
            m_aggregate = new Window(m_windows);
        }

        public float ComputeSimilarity(Window window, OHLCOptions ohlcOptions)
        {
            return 0.0f;
        }

    }
}
