using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Data
{
    public class EventMsgHandle_Response:EventMsgHandle_Base
    {
        public EventMsgHandle_Response(ModbusMessage msgModbus)
        {
            this.type = TYPE_RESPOND;
            this.msgModbus = msgModbus;
        }
        public EventMsgHandle_Response(ModbusMessage msgModbus,int percentExcute)
        {
            this.type = TYPE_RESPOND;
            this.msgModbus = msgModbus;
            this.percentExcute = percentExcute;
        }
        public ModbusMessage MsgResponse
        {
            get { return this.msgModbus; }
        }
        public int PercentExcute
        {
            get { return this.percentExcute; }
        }
        private ModbusMessage msgModbus;
        private int percentExcute;
    }
}
