using KModbus.Interfaces;
using KModbus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message
{
    public class ReadWriteRegisterRequest : IModbusRequest
    {

        public byte SlaverAddress { get; set; }
        public byte FuntionCode => ModbusFunctionCodes.ReadWriteRegister;
        public ushort ReadAddress { get; set; }
        public ushort NumberReadRegister { get; set; }
        public ushort WriteAddress { get; set; }
        public ushort[] DataRegisterWrite { get; set; }

        public ReadWriteRegisterRequest(byte slaverAddress, ushort readAddress, 
                                ushort numberReadRegister, ushort writeAddress, ushort[] dataRegisterWrite)
        {
            SlaverAddress = slaverAddress;
            ReadAddress = readAddress;
            NumberReadRegister = numberReadRegister;
            WriteAddress = writeAddress;
            DataRegisterWrite = dataRegisterWrite;
        }

    }
}
