using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Event
{
    public class MsgCountQueueModbus_EventArg : EventArgs
    {
        public MsgCountQueueModbus_EventArg(int countQueue)
        {
            this.CountQueue = countQueue;
        }
        public int CountQueue { get; set; }
    }
}
