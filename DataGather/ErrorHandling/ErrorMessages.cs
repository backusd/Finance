using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGather.ErrorHandling
{
    public static class ErrorMessages
    {
        private static List<string> m_errorMessages;

        public static void Initialize()
        {
            m_errorMessages = new List<string>();
        }

        public static void AddErrorMessage(string message)
        {
            m_errorMessages.Add(message);
        }
    }
}
