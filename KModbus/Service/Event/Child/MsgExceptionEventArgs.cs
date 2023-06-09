using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Event.Child
{
    public class MsgExceptionEventArgs:EventArgs
    {
        public MsgExceptionEventArgs(Exception ex,object sender) 
        {
            Ex = ex;
            Sender = sender;
        }
        public Exception Ex { get; private set; }
        public object Sender { get; private set; }
    }
}
