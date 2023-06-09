using KModbus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message
{
    public class ReadHoldingRegisterResponse : IModbusResponse
    {
        public byte SlaverAddress { get; set ; }

        public byte FuntionCode => ModbusFunctionCodes.ReadHoldingRegisters;

        public UInt16[] Register { get; set; }


    }
}
