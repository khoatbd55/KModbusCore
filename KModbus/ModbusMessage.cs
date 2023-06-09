using KModbus.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus
{
    public class ModbusMessage
    {
        public ModbusMessage(IModbusRequest request,IModbusResponse response)
        {
            this.Request = request;
            this.Response = response;
        }
        public IModbusRequest Request { get; set; }
        public IModbusResponse Response { get; set; }

    }
}
