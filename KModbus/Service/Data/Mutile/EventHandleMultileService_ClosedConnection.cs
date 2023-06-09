using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Data.Mutile
{
    public class EventHandleMultileService_ClosedConnection:EventHandleMultileService_Base
    {
        public EventHandleMultileService_ClosedConnection(string nameComport)
        {
            this.type = ETypeEventMultileService.ClosedConnection;
            this.nameComport = nameComport;
        }
    }
}
