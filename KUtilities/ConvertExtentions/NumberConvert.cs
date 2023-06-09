using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUtilities.ConvertExtentions
{
    public class NumberConvert
    {
        public static int BCDToInt(int Hex)
        {
            return (Hex >> 4) * 10 + (Hex & 0x0F);
        }

        public static int IntToBCD(int Int)
        {
            return ((Int / 10) << 4) | (Int % 10);
        }
    }
}
