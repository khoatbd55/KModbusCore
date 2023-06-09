using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Ex
{
    public class ModbusCommunicationTimedOutException : ModbusCommunicationException
    {
        public ModbusCommunicationTimedOutException() : base("The operation has timed out.")
        {
        }

        public ModbusCommunicationTimedOutException(Exception innerException) : base("The operation has timed out.", innerException)
        {
        }
    }
}
