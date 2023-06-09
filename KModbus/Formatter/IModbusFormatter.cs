using KModbus.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Formatter
{
    internal interface IModbusFormatter
    {
        byte[] Create(IModbusRequest request);
        IModbusResponse DecodeMessage(byte[] frameResponse);
    }
}
