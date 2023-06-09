using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Ex
{
    public class FrameModbusDecodeException:Exception
    {
        public FrameModbusDecodeException(string message) : base(message)
        {

        }

        public FrameModbusDecodeException(string message, Exception ex) : base(message, ex)
        {

        }
    }
}
