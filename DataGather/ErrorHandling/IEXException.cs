using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGather.ErrorHandling
{
    public class IEXException : Exception
    {
        private bool m_tooManyCalls;

        public IEXException(bool tooManyCalls = false)
        {
            m_tooManyCalls = tooManyCalls;
        }

        public IEXException(string message, bool tooManyCalls = false)
            : base(message)
        {
            m_tooManyCalls = tooManyCalls;
        }

        public IEXException(string message, Exception inner, bool tooManyCalls = false)
            : base(message, inner)
        {
            m_tooManyCalls = tooManyCalls;
        }

        public bool ToManyCalls()
        {
            return m_tooManyCalls;
        }
    }
}
