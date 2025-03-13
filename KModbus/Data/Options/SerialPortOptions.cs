using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Data.Options
{
    public class SerialPortOptions
    {
        public int Baudrate { get; set; } = 9600;
        public string PortName { get; set; }
        public Parity Parity { get; set; } = Parity.None;
        public int DataBit { get; set; } = 8;
        public StopBits StopBit { get; set; } = StopBits.One;
    }
}
