using KModbus.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Event
{
    public class MsgNoResponseModbus_EventArg:EventArgs
    {
        public MsgNoResponseModbus_EventArg()
        {

        }
        public MsgNoResponseModbus_EventArg(IModbusRequest request, object sender)
        {
            this.Request = request;
            Sender = sender;
        }
        public IModbusRequest Request { get; set; }
        public object Sender { get; private set; }
    }
}
