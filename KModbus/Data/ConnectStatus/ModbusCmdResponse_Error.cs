using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Data
{
    public class ModbusCmdResponse_Error<T>:ModbusCmdResponse_Base<T>
    {
        public ModbusCmdResponse_Error(T obj)
        {
            this.Type = EModbusCmdResponseType.Error;
            this.ResultObj = obj;
            this.Message = "Error Service";
        }
        public ModbusCmdResponse_Error()
        {
            this.Type = EModbusCmdResponseType.Error;
            this.Message = "Error Service";
        }
    }
}
