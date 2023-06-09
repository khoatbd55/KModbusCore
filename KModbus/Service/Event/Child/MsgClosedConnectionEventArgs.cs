using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Event.Child
{
    public class MsgClosedConnectionEventArgs:EventArgs
    {
        public MsgClosedConnectionEventArgs(EventArgs args,object sender) 
        {
            Args=args;
            Sender=sender;
        }

        public EventArgs Args { get;private set; }
        public object Sender { get;private set; }
    }
}
