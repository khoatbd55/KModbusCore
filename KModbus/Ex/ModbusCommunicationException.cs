using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Ex
{
    public class ModbusCommunicationException : Exception
    {
        protected ModbusCommunicationException()
        {

        }

        public ModbusCommunicationException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }

        public ModbusCommunicationException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
