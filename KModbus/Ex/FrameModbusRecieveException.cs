using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Ex
{
    public class FrameModbusRecieveException:Exception
    {
        public FrameModbusRecieveException(string message) : base(message)
        {

        }

        public FrameModbusRecieveException(string message, Exception ex) : base(message, ex)
        {

        }
    }
}
