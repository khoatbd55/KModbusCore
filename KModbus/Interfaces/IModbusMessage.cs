using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Interfaces
{
    public interface IModbusMessage
    {
        byte SlaverAddress { get; set; }
        byte FuntionCode { get;}

    }
}
