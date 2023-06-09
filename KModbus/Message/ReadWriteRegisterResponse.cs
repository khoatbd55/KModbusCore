using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message
{
    public class ReadWriteRegisterResponse : IModbusResponse
    {
        public byte SlaverAddress { get; set; }
        public byte FuntionCode => ModbusFunctionCodes.ReadWriteRegister;
        public ushort[] Register { get; set; }

    }
}
