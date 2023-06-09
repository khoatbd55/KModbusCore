using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.IO
{
    public class MsgComportRecv
    {
        public MsgComportRecv()
        {

        }
        public MsgComportRecv(byte[] frameRecv)
        {
            this.frameMessage = frameRecv;
        }
        public byte[] frameMessage { get; set; }
    }
}
