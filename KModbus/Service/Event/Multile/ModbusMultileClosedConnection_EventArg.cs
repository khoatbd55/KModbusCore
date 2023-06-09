using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Event.Multile
{
    public class ModbusMultileClosedConnection_EventArg
    {
        public ModbusMultileClosedConnection_EventArg(string nameComport)
        {
            this.NameComport = nameComport;
        }
        public string NameComport { get; set; }
    }
}
