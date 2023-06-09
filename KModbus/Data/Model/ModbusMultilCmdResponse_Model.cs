using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Data
{
    public class ModbusMultilCmdResponse_Model
    {
        public string NameComport { get; set; }
        public ModbusMessage MsgModbus { get; set; }
    }
}
