using KModbus.Service.Model;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Config
{
    public class KModbusMasterOption
    {
        public string NameComport { get; set; }
        public int MsSleep { get; set; } = 10;
        public List<CommandModbus_Service> ListCmd { get; set; } = new List<CommandModbus_Service>();
        public int DelayResponse { get; set; } = 10;
        public int Baudrate { get; set; } = 9600;
        public bool IsAutoReconnect { get; set; } = true;
        public int WaitResponse { get; set; } = 1000;
        public Parity Parity { get; set; } = Parity.None;
        public int DataBit { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;

        public KModbusMasterOption()
        {
            IsAutoReconnect = true;
        }

        public KModbusMasterOption(string nameComport, int msSleep, List<CommandModbus_Service> listCmd, int delayResponse, int baudRate)
        {
            this.NameComport = nameComport;
            this.MsSleep = msSleep;
            this.ListCmd = listCmd;
            this.DelayResponse = delayResponse;
            this.Baudrate = baudRate;
        }

        public KModbusMasterOption(string nameComport, int msSleep, List<CommandModbus_Service> listCmd, int delayResponse, int baudRate, int waitResponse)
        {
            this.NameComport = nameComport;
            this.MsSleep = msSleep;
            this.ListCmd = listCmd;
            this.DelayResponse = delayResponse;
            this.Baudrate = baudRate;
            this.WaitResponse = waitResponse;
        }

        public KModbusMasterOption(string nameComport, int baudrate)
        {
            this.NameComport = nameComport;
            this.Baudrate = baudrate;
            this.ListCmd = new List<CommandModbus_Service>();
        }

        public KModbusMasterOption(string nameComport)
        {
            this.NameComport = nameComport;
            this.Baudrate = 9600;
            this.MsSleep = 5;
            this.ListCmd = new List<CommandModbus_Service>();
            this.DelayResponse = 5;
        }
    }
}
