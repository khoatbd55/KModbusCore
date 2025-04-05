using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Extention
{
    public static class EnumExtention
    {
#if NETCOREONLY
        public static Mono.IO.Ports.StopBits Convert(this StopBits stopbit)
        {
            if (stopbit == StopBits.None)
            {
                return Mono.IO.Ports.StopBits.None;
            }
            else if(stopbit==StopBits.One)
            {
                return Mono.IO.Ports.StopBits.One;
            }
            else
            {
                return Mono.IO.Ports.StopBits.Two;
            }
        }

        public static Mono.IO.Ports.Parity Convert(this Parity parity)
        {
            if(parity == Parity.None)
            {
                return Mono.IO.Ports.Parity.None;
            }
            else if(parity==Parity.Odd)
            {
                return Mono.IO.Ports.Parity.Odd;
            }
            else
            {
                return Mono.IO.Ports.Parity.Even;
            }

        }


#endif
    }
}
