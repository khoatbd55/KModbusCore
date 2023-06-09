using KModbus.Service.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Data.Mutile
{
    public class EventHandleMultileService_Response:EventHandleMultileService_Base
    {
        public EventHandleMultileService_Response(string nameComport, MsgResponseModbus_EventArg response)
        {
            this.type = ETypeEventMultileService.Response;
            this.nameComport = nameComport;
            this.msgResponse = response;
        }

        public MsgResponseModbus_EventArg MsgResponse
        {
            get { return this.msgResponse; }
        }

        private MsgResponseModbus_EventArg msgResponse;
        
    }
}
