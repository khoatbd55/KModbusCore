using KModbus.Service.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Data.Mutile
{
    public class EventHandleMultileService_NoResponse: EventHandleMultileService_Base
    {

        public EventHandleMultileService_NoResponse(string nameComport, MsgNoResponseModbus_EventArg noResponse)
        {
            this.nameComport = nameComport;
            this.type = ETypeEventMultileService.NoResponse;
            this.msgNoResponse = noResponse;
        }

        private MsgNoResponseModbus_EventArg msgNoResponse;
        public MsgNoResponseModbus_EventArg MsgNoResponse 
        { 
            get { return this.msgNoResponse; }
        }
    }
}
