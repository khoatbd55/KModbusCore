using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message
{
    public class PresetMutipleRegisterResponse : IModbusResponse
    {
        public byte SlaverAddress { get ; set ; }
        public byte FuntionCode => ModbusFunctionCodes.PresetMutipleRegister;
        public UInt16 AddressRegister { get; set; }
        public UInt16 NoOfRegister { get; set; }
    }
}
