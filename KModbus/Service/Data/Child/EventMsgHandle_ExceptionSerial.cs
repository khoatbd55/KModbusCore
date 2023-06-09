using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Data
{
    public class EventMsgHandle_ExceptionSerial : EventMsgHandle_Base
    {
        public EventMsgHandle_ExceptionSerial(Exception ex)
        {

            this.type = TYPE_EXCEPTION_COMPORT;
            this.Ex = ex;
        }

        public Exception Ex { get; set; }
    }
}
