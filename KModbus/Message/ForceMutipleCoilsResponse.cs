using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message
{
    public class ForceMutipleCoilsResponse : IModbusResponse
    {
        public byte SlaverAddress { get ; set ; }

        public byte FuntionCode => ModbusFunctionCodes.ForceMutiplsCoils;
        public UInt16 CoidAddress { get; set; }
        public UInt16 QuantityCoil { get; set; }

    }
}
