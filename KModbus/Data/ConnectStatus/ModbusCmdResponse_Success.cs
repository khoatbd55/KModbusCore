using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Data
{
    public class ModbusCmdResponse_Success<T>:ModbusCmdResponse_Base<T>
    {
        public ModbusCmdResponse_Success(T obj)
        {
            this.Type = EModbusCmdResponseType.Success;
            this.ResultObj = obj;
            this.Message = "Node Response";
        }
    }
}
