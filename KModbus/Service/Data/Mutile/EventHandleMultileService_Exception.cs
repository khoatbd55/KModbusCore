using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Data.Mutile
{
    public class EventHandleMultileService_Exception:EventHandleMultileService_Base
    {
        public EventHandleMultileService_Exception(string nameComport,Exception ex)
        {
            this.type = ETypeEventMultileService.ExceptionOccur;
            this.nameComport = nameComport;
            this.exOccur = ex;
        }

        public Exception ExOccur
        {
            get { return this.exOccur; }
        }
        private Exception exOccur;
    }
}
