using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message
{
    public class ReadInputStatusResponse : IModbusResponse
    {
        public byte SlaverAddress { get; set; }
        public byte FuntionCode => ModbusFunctionCodes.ReadInputStauts;
        public UInt16[] Register { get; set; }

    }
}
