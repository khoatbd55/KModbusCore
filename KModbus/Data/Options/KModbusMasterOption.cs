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
        public string NameId { get; set; }
        public int MsSleep { get; set; } = 10;
        public List<CommandModbus_Service> ListCmd { get; set; } = new List<CommandModbus_Service>();
        public int DelayResponse { get; set; } = 10;
        public bool IsAutoReconnect { get; set; } = true;
        public int WaitResponse { get; set; } = 1000;

        public KModbusMasterOption( int msSleep, List<CommandModbus_Service> listCmd, int delayResponse)
        {
            this.MsSleep = msSleep;
            this.ListCmd = listCmd;
            this.DelayResponse = delayResponse;
        }

        public KModbusMasterOption( int msSleep, List<CommandModbus_Service> listCmd, int delayResponse, int waitResponse)
        {
            this.MsSleep = msSleep;
            this.ListCmd = listCmd;
            this.DelayResponse = delayResponse;
            this.WaitResponse = waitResponse;
        }



        public KModbusMasterOption()
        {
            this.MsSleep = 5;
            this.ListCmd = new List<CommandModbus_Service>();
            this.DelayResponse = 5;
            IsAutoReconnect = true;
        }
    }
}
