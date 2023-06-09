using KModbus.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Data
{
    public class EventMsgHandle_NoResponse:EventMsgHandle_Base
    {
        public EventMsgHandle_NoResponse(IModbusRequest request)
        {
            this.type = TYPE_NO_RESPOND;
            this.request = request;
        }
        public IModbusRequest Command_Request
        {
            get { return this.request; }
        }
        private IModbusRequest request;
    }
}
