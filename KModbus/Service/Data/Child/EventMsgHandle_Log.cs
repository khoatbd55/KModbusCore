using KModbus.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Data.Child
{
    internal class EventMsgHandle_Log: EventMsgHandle_Base
    {
        public EModbusLogType LogType { get; private set; }
        public string Message { get; private set; }
        public Exception Ex { get;private set; }

        public EventMsgHandle_Log(EModbusLogType logType, string message)
        {
            this.type = TYPE_LOG;
            LogType = logType;
            Message = message;
        }
        public EventMsgHandle_Log(EModbusLogType logType, string message, Exception ex):this(logType,message)
        {
            Ex = ex;
        }

    }
}
