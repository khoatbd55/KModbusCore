using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Data
{
    public class EventMsgHandle_Base
    {
        public const int TYPE_RESPOND = 0;
        public const int TYPE_NO_RESPOND = 1;
        public const int TYPE_STOP = 2;
        public const int TYPE_EXCEPTION_COMPORT = 3;
        public const int TYPE_LOG = 4;

        public int Type
        {
            get { return this.type; }
        }
        protected int type;
    }
}
