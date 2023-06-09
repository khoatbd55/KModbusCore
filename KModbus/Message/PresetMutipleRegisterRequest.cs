using KModbus.Interfaces;
using KModbus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message
{
    public class PresetMutipleRegisterRequest : IModbusRequest
    {
        public PresetMutipleRegisterRequest(byte slaverAddress, UInt16 addressRegister, UInt16[] dataReg)
        {
            this.SlaverAddress = slaverAddress;
            this.AddressRegister = addressRegister;
            this.DataReg = dataReg; 
        }
        public byte SlaverAddress { get; set; }
        public byte FuntionCode => ModbusFunctionCodes.PresetMutipleRegister;
        public UInt16 AddressRegister { get; set; }
        public UInt16[] DataReg { get; set; }

    }
}
