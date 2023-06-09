using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message
{
    public class ReadInputRegisterResponse : IModbusResponse
    {
        public byte SlaverAddress { get; set ; }

        public byte FuntionCode => ModbusFunctionCodes.ReadInputRegisters;
        public UInt16[] Register { get; set; }

    }
}
