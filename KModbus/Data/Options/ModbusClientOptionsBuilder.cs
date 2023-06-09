using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Data.Options
{
    public class ModbusClientOptionsBuilder
    {
        private ModbusClientOptions _options;
        public ModbusClientOptions Build()
        {
            if (_options.TcpOption == null)
                _options.TcpOption = new ModbusClientTcpOptions();
            return _options;
        }

        public ModbusClientOptionsBuilder WithTcpIp(string server,int port,int milisecSleep,int delayResponse,int timeOut, ushort transactionId)
        {
            _options = new ModbusClientOptions();
            _options.TransactionId = transactionId; 
            _options.PacketProtocal = EModbusPacketProtocal.TcpIp;
            _options.MilisecSleep= milisecSleep;
            _options.DelayResponse = delayResponse;
            _options.TimeOutConnect = timeOut;
            _options.TcpOption = new ModbusClientTcpOptions()
            {
                Server=server,
                Port=port
            };
            return this;
        }

        public ModbusClientOptionsBuilder WithTcpIp(string server, int port, int milisecSleep, int delayResponse, ushort transactionId)
        {
            return WithTcpIp(server, port, milisecSleep, delayResponse, 30, transactionId);
        }

        public ModbusClientOptionsBuilder WithTcpIp(string server, int port, int milisecSleep, ushort transactionId)
        {
            return WithTcpIp(server, port, milisecSleep, 50, 30, transactionId);
        }
        public ModbusClientOptionsBuilder WithTcpIp(string server, int port, ushort transactionId)
        {
            return WithTcpIp(server, port, 10, 50, 30, transactionId);
        }

        public ModbusClientOptionsBuilder WithRtuOverTcpIp(string server,int port, int milisecSleep,int delayResponse,int timeOut)
        {
            _options = new ModbusClientOptions();
            _options.PacketProtocal = EModbusPacketProtocal.RtuOverTcpIp;
            _options.MilisecSleep = milisecSleep;
            _options.DelayResponse = delayResponse;
            _options.TimeOutConnect = timeOut;
            _options.TcpOption = new ModbusClientTcpOptions()
            {
                Server = server,
                Port = port
            };
            return this;
        }

        public ModbusClientOptionsBuilder WithRtuOverTcpIp(string server, int port, int milisecSleep, int delayResponse)
        {
            return WithRtuOverTcpIp(server, port, milisecSleep, delayResponse, 30);
        }

        public ModbusClientOptionsBuilder WithRtuOverTcpIp(string server, int port, int milisecSleep)
        {
            return WithRtuOverTcpIp(server, port, milisecSleep, 50, 30);
        }

        public ModbusClientOptionsBuilder WithRtuOverTcpIp(string server, int port)
        {
            return WithRtuOverTcpIp(server, port, 10, 50, 30);
        }
    }
}
