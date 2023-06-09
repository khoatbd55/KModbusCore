using KModbus.Interfaces;
using KModbus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message
{
    public class ForceMutipleCoilsRequest : IModbusRequest
    {
        public byte SlaverAddress { get; set; }

        public byte FuntionCode => ModbusFunctionCodes.ForceMutiplsCoils;
        public UInt16 CoilAddress { get; set; }
        public UInt16 QuantityColis { get;set; }
        public UInt16[] CoilData { get; set; }
    }
}
