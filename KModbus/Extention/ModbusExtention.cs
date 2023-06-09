using KModbus.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Extention
{
    public class ModbusExtention
    {
        // tính toán chiều dài frame trả lời từ frame yêu cầu
        public static int CalulatorFrameLenResponse_FromRequest(IModbusRequest request)
        {
            int result = 8;// chiều dài frame nhỏ nhất là 8 byte 
            switch(request.FuntionCode)
            {
                case ModbusFunctionCodes.ReadCoilStatus:
                    {
                        var read_coil = (ReadCoilStatusRequest)request;
                        var how_many_byte = 5;
                        how_many_byte += read_coil.NoOfPoints / 8;
                        if (read_coil.NoOfPoints % 8 != 0)
                            how_many_byte++;
                        result = how_many_byte;
                    }
                    break;
                case ModbusFunctionCodes.ReadInputStauts:
                    {
                        var read_input = (ReadInputStatusRequest)request;
                        var how_many_byte = 5;
                        how_many_byte += read_input.NoOfPoints / 8;
                        if (read_input.NoOfPoints % 8 != 0)
                            how_many_byte++;
                        result = how_many_byte;
                    }
                    break;
                case ModbusFunctionCodes.ReadHoldingRegisters:
                    {
                        var read_holding = (ReadHoldingRegisterRequest)request;
                        var how_many_byte = 5;
                        how_many_byte += read_holding.NoOfPoints * 2;
                        result = how_many_byte;
                    }
                    break;
                case ModbusFunctionCodes.ReadInputRegisters:
                    {
                        var read_input = (ReadInputRegisterRequest)request;
                        var how_many_byte = 5;
                        how_many_byte += read_input.NoOfPoints * 2;
                        result = how_many_byte;
                    }
                    break;
                case ModbusFunctionCodes.WriteSingleCoil:
                case ModbusFunctionCodes.ForceMutiplsCoils:
                case ModbusFunctionCodes.PresetSingleRegister:
                case ModbusFunctionCodes.PresetMutipleRegister:
                    result = 8;
                    break;
                case ModbusFunctionCodes.WriteGenernal:
                    break;

            }    
            return result;
        }
    }
}
