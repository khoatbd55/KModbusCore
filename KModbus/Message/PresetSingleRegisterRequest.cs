using KModbus.Interfaces;
using KModbus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message
{
    public class PresetSingleRegisterRequest : IModbusRequest
    {
        public byte SlaverAddress { get; set ; }
        public byte FuntionCode => ModbusFunctionCodes.PresetSingleRegister;
        public UInt16 RegisterAddress { get; set; }
        public UInt16 PresetData { get; set; }

    }
}
