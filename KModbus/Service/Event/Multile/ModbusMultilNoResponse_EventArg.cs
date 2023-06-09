using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Event.Multile
{
    public class ModbusMultilNoResponse_EventArg
    {
        public ModbusMultilNoResponse_EventArg(string nameComport, MsgNoResponseModbus_EventArg msgNoResponse)
        {
            this.NameComport = nameComport;
            this.MsgNoResponse = msgNoResponse;
        }
        public string NameComport { get; set; }
        public MsgNoResponseModbus_EventArg MsgNoResponse { get; set; }
    }
}
