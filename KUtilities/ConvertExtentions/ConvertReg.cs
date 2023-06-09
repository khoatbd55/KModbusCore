using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUtilities.ConvertExtentions
{
    public class ConvertReg
    {
        public static float[] ConvertArrayUin16ToFloat(UInt16[] reg)
        {
            if (reg.Length % 2 != 0)
                throw new Exception("Định dạng dữ liệu sai");
            byte[] b_float = new byte[reg.Length * 2];
            int idx = 0;
            for (int i = 0; i < reg.Length; i++)
            {
                b_float[idx++] = (byte)reg[i];
                b_float[idx++] = (byte)(reg[i] >> 8);
            }
            float[] f_result = new float[reg.Length / 2];
            idx = 0;
            for (int i = 0; i < f_result.Length; i++)
            {
                f_result[i] = BitConverter.ToSingle(b_float, idx);
                idx += 4;
            }
            return f_result;
        }
        public static UInt16[] ConvertArrayFloatToUInt16(List<float> list_float)
        {
            var b_reg = new byte[list_float.Count * 4];
            int idx = 0;
            for (int i = 0; i < list_float.Count; i++)
            {
                var b_float = BitConverter.GetBytes(list_float[i]);
                Array.Copy(b_float, 0, b_reg, idx, 4);
                idx += 4;
            }
            var regs = ConvertArrayByteToUnit16(b_reg);
            return regs;

        }

        public static byte[] ConvertArrayUint16ToByte(UInt16[] register)
        {
            byte[] b_reg = new byte[register.Length * 2];
            int idx = 0;
            for (int i = 0; i < register.Length; i++)
            {
                b_reg[idx++] = (byte)register[i];
                b_reg[idx++] = (byte)(register[i] >> 8);
            }
            return b_reg;
        }
        public static UInt16[] ConvertArrayByteToUnit16(byte[] b_reg)
        {
            UInt16[] reg_cache = new UInt16[b_reg.Length / 2];
            int idx = 0;
            for (int i = 0; i < reg_cache.Length; i++)
            {
                reg_cache[i] = BitConverter.ToUInt16(b_reg, idx);
                idx += 2;
            }
            return reg_cache;
        }
    }
}
