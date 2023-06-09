using KModbus.Interfaces;
using KModbus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message
{
    public class ReadInputStatusRequest : IModbusRequest
    {

        public ReadInputStatusRequest(byte slave_address,UInt16 address_coil,UInt16 no_of_points)
        {
            this.SlaverAddress = slave_address;
            this.AddressCoil = address_coil;
            this.NoOfPoints = no_of_points;
        }
        public byte SlaverAddress { get; set; }
        public byte FuntionCode => ModbusFunctionCodes.ReadInputStauts;
        public UInt16 AddressCoil { get; set; }
        public UInt16 NoOfPoints { get; set; }

    }
}
