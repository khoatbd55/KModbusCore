using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Utils
{
    public class ModbusUtils
    {
        public static int CalculateCRC(byte[] frame, int frame_size)
        {
            int temp, temp2, flag;
            temp = 0xFFFF;
            for (int i = 0; i < frame_size; i++)
            {
                temp = temp ^ frame[i];
                for (int j = 1; j <= 8; j++)
                {
                    flag = temp & 0x0001;
                    temp >>= 1;
                    if (flag != 0)
                        temp ^= 0xA001;
                }
            }
            // Reverse byte order. 
            temp2 = temp >> 8;
            temp = (temp << 8) | temp2;
            temp &= 0xFFFF;
            // the returned value is already swapped
            // crcLo byte is first & crcHi byte is last
            return temp;
        }
    }
}
