using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Data.Options
{
    public class ModbusClientOptions
    {
        public ushort TransactionId { get; set; }
        public int MilisecSleep { get; set; }
        public int DelayResponse { get; set; }
        public int TimeOutConnect { get; set; }
        public EModbusPacketProtocal PacketProtocal { get; set; }
        public ModbusClientTcpOptions TcpOption { get; set; }
        public ModbusClientOptions()
        {
            TransactionId = 1;
            PacketProtocal = EModbusPacketProtocal.TcpIp;
            TcpOption = new ModbusClientTcpOptions();
            MilisecSleep = 10;
            DelayResponse = 50;
            TimeOutConnect = 30;
        }

    }
}
