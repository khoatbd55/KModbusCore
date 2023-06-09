using KModbus.Interfaces;
using KModbus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message
{
    public class ReadCoilStatusRequest : IModbusRequest
    {
        public ReadCoilStatusRequest(byte slave_adr,UInt16 address_coil,UInt16 no_of_point)
        {
            this.SlaverAddress = slave_adr;
            this.AddressCoil = address_coil;
            this.NoOfPoints = no_of_point;
        }

        public byte SlaverAddress { get ; set; }
        public byte FuntionCode => ModbusFunctionCodes.ReadCoilStatus;
        public UInt16 AddressCoil { get; set; }
        public UInt16 NoOfPoints { get; set; }
    }
}
