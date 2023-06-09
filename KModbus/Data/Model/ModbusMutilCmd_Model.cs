using KModbus.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Data
{
    public class ModbusMutilCmd_Model
    {
        public string NameComport { get; set; }
        public IModbusRequest Request { get; set; }
    }
}
