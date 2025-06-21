using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Data.Options
{
    public class ModbusClientTcpChannelOptions
    {
        public AddressFamily AddressFamily { get; set; } = AddressFamily.Unspecified;

        public int BufferSize { get; set; } = 8192;

        public bool? DualMode { get; set; }

        public LingerOption LingerState { get; set; } = new LingerOption(true, 0);

        public bool NoDelay { get; set; } = true;
        public int Port { get; set; }
        public string Server { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public ModbusClientTlsOptions TlsOptions { get; set; } = new ModbusClientTlsOptions();
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

        public ModbusClientTcpChannelOptions()
        {
            Port = 502;
            Server = "127.0.0.1";
        }

    }
}
