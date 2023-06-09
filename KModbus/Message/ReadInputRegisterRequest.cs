using KModbus.Interfaces;
using KModbus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message
{
    public class ReadInputRegisterRequest : IModbusRequest
    {
        public ReadInputRegisterRequest(byte slaverAddress,UInt16 addressRegister,UInt16 noOfPoints)
        {
            this.SlaverAddress = slaverAddress;
            this.AddressRegister = addressRegister;
            this.NoOfPoints = noOfPoints;
        }
        public byte SlaverAddress { get; set; }
        public byte FuntionCode => ModbusFunctionCodes.ReadInputRegisters;
        public UInt16 AddressRegister { get; set; }
        public UInt16 NoOfPoints { get; set; }

    }
}
