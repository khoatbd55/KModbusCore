using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Event.Multile
{
    public class ModbusMultileException_EventArg
    {
        public ModbusMultileException_EventArg(string nameComport,Exception ex)
        {
            NameComport = nameComport;
            Ex = ex;
        }

        public string NameComport { get; set; }
        public Exception Ex { get; set; }
    }
}
