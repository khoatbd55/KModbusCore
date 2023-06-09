using KModbus.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Events
{
    public class ModbusLogEventArgs
    {
        public EModbusLogType Type { get => _type; }
        public string Message { get => _message; }
        public Exception Ex { get => _ex; } 

        public ModbusLogEventArgs(EModbusLogType type,string message)
        {
            _type = type;
            _message = message;
        }

        public ModbusLogEventArgs(EModbusLogType type, string message, Exception ex) : this(type, message)
        {
            _ex = ex;
        }

        private EModbusLogType _type;
        private string _message;
        private Exception _ex;
    }
}
