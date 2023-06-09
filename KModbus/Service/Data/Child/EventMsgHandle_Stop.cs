using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Data
{
    public class EventMsgHandle_Stop:EventMsgHandle_Base
    {
        public EventMsgHandle_Stop()
        {
            this.type = TYPE_STOP;
        }
        public EventMsgHandle_Stop(string reason)
        {
            this.type = TYPE_STOP;
            this.reason = reason;
        }
        public string Reason
        {
            get { return this.reason; }
        }
        private string reason { get; set; }

    }
}
