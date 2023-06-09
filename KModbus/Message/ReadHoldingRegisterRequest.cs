using KModbus.Interfaces;
using KModbus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message
{
    public class ReadHoldingRegisterRequest : IModbusRequest
    {
        public ReadHoldingRegisterRequest(byte slaverAddress,UInt16 addressCoil,UInt16 noOfPoint)
        {
            this.SlaverAddress = slaverAddress;
            this.AddressRegister = addressCoil;
            this.NoOfPoints = noOfPoint;
        }

        public byte SlaverAddress { get;set ; }

        public byte FuntionCode => ModbusFunctionCodes.ReadHoldingRegisters;
        public UInt16 AddressRegister { get; set; }
        public UInt16 NoOfPoints { get; set; }

    }
}
