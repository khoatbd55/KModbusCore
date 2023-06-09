using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Data
{
    public class ModbusCmdResponse_Base<T>
    {
        public EModbusCmdResponseType Type { get; set; }
        public string Message { get; set; }
        public T ResultObj { get; set; }
    }
}
