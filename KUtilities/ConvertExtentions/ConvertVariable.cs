using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUtilities.ConvertExtentions
{
    public class ConvertVariable
    {
        public static void IntergerIntoArrayByte(double x, int gain, byte[] frame, ref int idx, int size)
        {
            int x_int = (int)(x * gain);
            var b_x_int = BitConverter.GetBytes(x_int);
            Array.Copy(b_x_int, 0, frame, idx, size);
            idx += size;
        }

        public static void IntergerIntoArrayByte(float x, byte[] frame, ref int idx)
        {
            var b_x_int = BitConverter.GetBytes(x);
            Array.Copy(b_x_int, 0, frame, idx, b_x_int.Length);
            idx += b_x_int.Length;
        }

        public static void IntergerIntoArrayByte(bool b, byte[] frame, ref int idx)
        {
            frame[idx] = (byte)(b ? 1 : 0);
            idx++;
        }
        public static bool ByteBitToBoolean(byte frame, int pos)
        {
            return ((frame & (1 << pos)) != 0x00) ? true : false;
        }

        public static bool ByteToBoolean(byte[] frame, ref int idx)
        {
            var result = frame[idx] != 0 ? true : false;
            idx++;
            return result;
        }

        public static UInt16 BytesToUInt16(byte[] frame, ref int idx)
        {
            var result = BitConverter.ToUInt16(frame, idx);
            idx += 2;
            return result;
        }

        public static Int16 BytesToInt16(byte[] frame, ref int idx)
        {
            var result = BitConverter.ToInt16(frame, idx);
            idx += 2;
            return result;
        }

        public static UInt16 BytesToSwapUInt16(byte[] frame, ref int idx)
        {
            var result = (UInt16)(((int)frame[idx]) << 8 | frame[idx + 1]);
            idx += 2;
            return result;
        }

        public static UInt32 BytesToUInt32(byte[] frame, ref int idx)
        {
            var result = BitConverter.ToUInt32(frame, idx);
            idx += 4;
            return result;
        }


        public static Int32 BytesToInt32(byte[] frame, ref int idx)
        {
            var result = BitConverter.ToInt32(frame, idx);
            idx += 4;
            return result;
        }

        public static float BytesToFloat(byte[] frame, ref int idx)
        {
            var result = BitConverter.ToSingle(frame, idx);
            idx += 4;
            return result;
        }

        public static double BytesToDouble(byte[] frame,ref int idx)
        {
            var result = BitConverter.ToDouble(frame, idx);
            idx += 8;
            return result;
        }

        public static float BytesToFloat(byte[] frame, ref int idx, int size, int scale)
        {
            if (size <= 2)
            {
                var result = (float)(BitConverter.ToUInt16(frame, idx)) / scale;
                idx += size;
                return result;
            }
            else
            {
                var result = (float)(BitConverter.ToUInt32(frame, idx)) / scale;
                idx += size;
                return result;
            }
        }
        /// <summary>
        /// chuyển byte sang số nguyên rồi chia hệ số sáng số thưc
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="idx"></param>
        /// <param name="size"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static double BytesToIntDouble(byte[] frame, ref int idx, int size, int scale)
        {

            if (size <= 2)
            {
                var result = (double)(BitConverter.ToUInt16(frame, idx)) / scale;
                idx += size;
                return result;
            }
            else if (size <= 4)
            {
                var result = (double)(BitConverter.ToUInt32(frame, idx)) / scale;
                idx += size;
                return result;
            }
            else
            {
                var result = (double)(BitConverter.ToUInt64(frame, idx)) / scale;
                idx += size;
                return result;
            }
        }
    }
}
