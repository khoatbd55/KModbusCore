using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Data.Options
{
    public class ModbusClientTcpOptions
    {
        public ushort TransactionId { get; set; }
        public int TimeOutConnect { get; set; }
        public EModbusPacketProtocal PacketProtocal { get; set; }
        public ModbusClientTcpChannelOptions TcpOption { get; set; }
        public ModbusClientTcpOptions()
        {
            TransactionId = 1;
            PacketProtocal = EModbusPacketProtocal.TcpIp;
            TcpOption = new ModbusClientTcpChannelOptions();
            TimeOutConnect = 30;
        }

    }
}
