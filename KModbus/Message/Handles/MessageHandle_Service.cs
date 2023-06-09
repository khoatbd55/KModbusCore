using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message.Handles
{
    public class MessageHandle_Service
    {
        private const int crc_len = 2;
        public static int GetRtuRequestBytesToRead(byte[] frameStart)
        {
            byte funtionCode = frameStart[1];
            if (funtionCode > ModbusFunctionCodes.ExceptionOffset)
                return 1;
            int result = 0;
            switch (funtionCode)
            {
                case ModbusFunctionCodes.ReadCoilStatus:
                case ModbusFunctionCodes.ReadInputStauts:
                case ModbusFunctionCodes.ReadHoldingRegisters:
                case ModbusFunctionCodes.ReadInputRegisters:
                case ModbusFunctionCodes.ReadWriteRegister:
                    result = frameStart[2] + 1 ;// 
                    break;
                case ModbusFunctionCodes.PresetSingleRegister:
                case ModbusFunctionCodes.ForceMutiplsCoils:
                case ModbusFunctionCodes.PresetMutipleRegister:
                    result = 4 ;
                    break;
            }
            return result;
        }
        public static byte[] ConcatFrame(byte[] frameStart, byte[] frameEnd)
        {
            int len_frame = frameStart.Length + frameEnd.Length;
            byte[] frame = new byte[len_frame];
            Array.Copy(frameStart, 0, frame, 0, frameStart.Length);
            Array.Copy(frameEnd, 0, frame, frameStart.Length, frameEnd.Length);
            return frame;
        }


    }
}
