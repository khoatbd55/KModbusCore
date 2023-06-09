using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Event
{
    public class MsgResponseModbus_EventArg:EventArgs
    {
        public MsgResponseModbus_EventArg(ModbusMessage msg_event, object sender)
        {
            this.msgModbus = msg_event;
            this.timeOccure = DateTime.Now;
            Sender = sender;
        }
        public DateTime TimeOccure
        {
            get { return this.timeOccure; }
        }
        public ModbusMessage Message
        {
            get { return this.msgModbus; }
        }

        public object Sender { get; private set; }
        
        private DateTime timeOccure;
        private ModbusMessage msgModbus;
    }
}
