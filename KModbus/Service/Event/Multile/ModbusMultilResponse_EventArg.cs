using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Event.Multile
{
    public class ModbusMultilResponse_EventArg
    {

        public ModbusMultilResponse_EventArg(string nameComport, MsgResponseModbus_EventArg msgResponse)
        {
            this.NameComport = nameComport;
            this.MsgResponse = msgResponse;
        }

        public string NameComport { get; set; }
        public MsgResponseModbus_EventArg MsgResponse { get; set; }
    }
}
