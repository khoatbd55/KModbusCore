using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Data
{
    public class ModbusCmdResponse_NoResponse<T>:ModbusCmdResponse_Base<T>
    {
        public ModbusCmdResponse_NoResponse(T obj)
        {
            this.ResultObj = obj;
            this.Type = EModbusCmdResponseType.NoResponse;
            this.Message = "Thiết bị không phản hồi";
        }
        public ModbusCmdResponse_NoResponse()
        {
            this.Type = EModbusCmdResponseType.NoResponse;
            this.Message = "Thiết bị không phản hồi";
        }
    }
}
