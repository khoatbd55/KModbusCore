using KModbus.Ex;
using KModbus.Message;
using KModbus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Formatter
{
    internal class ModbusRtuFormatter : IModbusFormatter
    {
        private const int MIN_LEN_FRAME = 6;
        public byte[] Create(IModbusRequest request)
        {
            if (request is ReadCoilStatusRequest readCoil)
            {
                return ReadCoilStatus_CreateMessage(readCoil);
            }
            else if (request is ReadInputStatusRequest readInputs)
            {
                return ReadInputStatus_CreateMessage(readInputs);
            }
            else if (request is ReadHoldingRegisterRequest readHoldingRegister)
            {
                return ReadHoldingRegister_CreateMessage(readHoldingRegister);
            }
            else if (request is ReadInputRegisterRequest readInputRegister)
            {
                return ReadInputRegister_CreateMessage(readInputRegister);
            }
            else if (request is ForceMutipleCoilsRequest forceMutipleCoils)
            {
                return ForceMutipleCoils_CreateMessage(forceMutipleCoils);
            }
            else if (request is PresetSingleRegisterRequest presetSingleRegister)
            {
                return PresetSingleRegister_CreateMessage(presetSingleRegister);
            }
            else if (request is PresetMutipleRegisterRequest presetMultipleRegister)
            {
                return PresetMultipleRegister_CreateMessage(presetMultipleRegister);
            }
            else if (request is ReadWriteRegisterRequest readWriteRegister)
            {
                return ReadWriteRegister_CreateMessage(readWriteRegister);
            }
            return null;
        }

        public IModbusResponse DecodeMessage(byte[] frameResponse)
        {
            // kiểm tra chiều dài của frame
            if (frameResponse.Length < MIN_LEN_FRAME)
                throw new Exception("frame response too short");
            // kiểm tra crc 
            int lenbuffer = frameResponse.Length;
            int crc_receive = ((int)frameResponse[lenbuffer - 2]) << 8 | (int)frameResponse[lenbuffer - 1];
            int crc_calculator = ModbusUtils.CalculateCRC(frameResponse, lenbuffer - 2);
            if (crc_receive != crc_calculator)
            {
                throw new Exception("frame response crc error");
            }
            else // kiểm tra funtion 
            {
                byte functionCode = frameResponse[1];
                if (functionCode > 128)
                    throw new Exception("frame response error format");
                else
                {
                    if (functionCode == ModbusFunctionCodes.ReadCoilStatus)
                        return ReadCoilStatus_DecodeMessage(frameResponse);
                    else if (functionCode == ModbusFunctionCodes.ReadInputStauts)
                        return ReadInputStatus_DecodeMessage(frameResponse);
                    else if (functionCode == ModbusFunctionCodes.ReadHoldingRegisters)
                        return ReadHoldingRegister_DecodeMessage(frameResponse);
                    else if (functionCode == ModbusFunctionCodes.ReadInputRegisters)
                        return ReadInputRegister_DecodeMessage(frameResponse);
                    else if (functionCode == ModbusFunctionCodes.PresetSingleRegister)
                        return PresetSingleRegister_DecodeMessage(frameResponse);
                    else if (functionCode == ModbusFunctionCodes.ForceMutiplsCoils)
                        return ForceMutipleCoils_DecodeMessage(frameResponse);
                    else if (functionCode == ModbusFunctionCodes.PresetMutipleRegister)
                        return PresetMutipleRegister_DecodeMessage(frameResponse);
                    else if (functionCode == ModbusFunctionCodes.ReadWriteRegister)
                        return ReadWriteRegister_DecodeMessage(frameResponse);
                    else
                    {
                        throw new Exception("function modbus not correct");
                    }    
                }
            }
        }

        #region request
        private byte[] ReadCoilStatus_CreateMessage(ReadCoilStatusRequest request)
        {
            ReadCoilStatusRequest msg_read_coil = request;

            byte[] buf = new byte[8];
            buf[0] = msg_read_coil.SlaverAddress;
            buf[1] = msg_read_coil.FuntionCode;
            buf[2] = (byte)(msg_read_coil.AddressCoil >> 8);
            buf[3] = (byte)msg_read_coil.AddressCoil;
            buf[4] = (byte)(msg_read_coil.NoOfPoints >> 8);
            buf[5] = (byte)(msg_read_coil.NoOfPoints);
            int crc = ModbusUtils.CalculateCRC(buf, 6);
            buf[6] = (byte)(crc >> 8);
            buf[7] = (byte)crc;
            return buf;
        }
        private byte[] ReadInputStatus_CreateMessage(ReadInputStatusRequest request)
        {
            ReadInputStatusRequest msg_read_coil = request;
            byte[] buf = new byte[8];
            buf[0] = msg_read_coil.SlaverAddress;
            buf[1] = msg_read_coil.FuntionCode;
            buf[2] = (byte)(msg_read_coil.AddressCoil >> 8);
            buf[3] = (byte)msg_read_coil.AddressCoil;
            buf[4] = (byte)(msg_read_coil.NoOfPoints >> 8);
            buf[5] = (byte)(msg_read_coil.NoOfPoints);
            int crc = ModbusUtils.CalculateCRC(buf, 6);
            buf[6] = (byte)(crc >> 8);
            buf[7] = (byte)crc;
            return buf;
        }
        private byte[] ReadHoldingRegister_CreateMessage(ReadHoldingRegisterRequest request)
        {
            ReadHoldingRegisterRequest holding_request = request;
            byte[] buf = new byte[8];
            buf[0] = holding_request.SlaverAddress;
            buf[1] = holding_request.FuntionCode;
            buf[2] = (byte)(holding_request.AddressRegister >> 8);
            buf[3] = (byte)holding_request.AddressRegister;
            buf[4] = (byte)(holding_request.NoOfPoints >> 8);
            buf[5] = (byte)(holding_request.NoOfPoints);
            int crc = ModbusUtils.CalculateCRC(buf, 6);
            buf[6] = (byte)(crc >> 8);
            buf[7] = (byte)crc;
            return buf;
        }
        private byte[] ReadInputRegister_CreateMessage(ReadInputRegisterRequest request)
        {
            ReadInputRegisterRequest input_request = request;
            byte[] buf = new byte[8];
            buf[0] = input_request.SlaverAddress;
            buf[1] = input_request.FuntionCode;
            buf[2] = (byte)(input_request.AddressRegister >> 8);
            buf[3] = (byte)input_request.AddressRegister;
            buf[4] = (byte)(input_request.NoOfPoints >> 8);
            buf[5] = (byte)(input_request.NoOfPoints);
            int crc = ModbusUtils.CalculateCRC(buf, 6);
            buf[6] = (byte)(crc >> 8);
            buf[7] = (byte)crc;
            return buf;
        }
        private byte[] ForceMutipleCoils_CreateMessage(ForceMutipleCoilsRequest request)
        {
            ForceMutipleCoilsRequest cmd = request;
            int no_of_register = cmd.QuantityColis / 16;
            int no_of_byte = no_of_register * 2;
            if (cmd.QuantityColis % 16 > 0)
            {
                no_of_register++;
                if (cmd.QuantityColis % 16 > 8)
                    no_of_byte += 2;
                else
                    no_of_byte += 1;
            }
            int buffer_size = 9 + no_of_byte;
            byte[] buf = new byte[buffer_size];
            buf[0] = cmd.SlaverAddress;
            buf[1] = cmd.FuntionCode;
            buf[2] = (byte)(cmd.CoilAddress >> 8);
            buf[3] = (byte)(cmd.CoilAddress);
            buf[4] = (byte)(cmd.QuantityColis >> 8);
            buf[5] = (byte)cmd.QuantityColis;
            buf[6] = (byte)no_of_byte;// byte count;
            int idx = 7;
            for (int i = 0; i < no_of_register; i++)
            {
                buf[idx++] = (byte)(cmd.CoilData[i] >> 8);
                buf[idx++] = (byte)(cmd.CoilData[i]);
            }
            // calculate crc
            int crc = ModbusUtils.CalculateCRC(buf, buffer_size - 2);
            buf[buffer_size - 2] = (byte)(crc >> 8);
            buf[buffer_size - 1] = (byte)crc;
            return buf;
        }
        private byte[] PresetSingleRegister_CreateMessage(PresetSingleRegisterRequest request)
        {
            PresetSingleRegisterRequest write_single = request;
            byte[] buf = new byte[8];
            buf[0] = write_single.SlaverAddress;
            buf[1] = write_single.FuntionCode;
            buf[2] = (byte)(write_single.RegisterAddress >> 8);
            buf[3] = (byte)write_single.RegisterAddress;
            buf[4] = (byte)(write_single.PresetData >> 8);
            buf[5] = (byte)(write_single.PresetData);
            int crc = ModbusUtils.CalculateCRC(buf, 6);
            buf[6] = (byte)(crc >> 8);
            buf[7] = (byte)crc;
            return buf;
        }
        private byte[] PresetMultipleRegister_CreateMessage(PresetMutipleRegisterRequest request)
        {
            PresetMutipleRegisterRequest cmd = request;
            int no_of_byte = cmd.DataReg.Length * 2;
            int no_of_register = cmd.DataReg.Length;
            int frame_size = 9 + no_of_byte;
            byte[] buf = new byte[frame_size];
            buf[0] = cmd.SlaverAddress;
            buf[1] = cmd.FuntionCode;
            buf[2] = (byte)(cmd.AddressRegister >> 8);
            buf[3] = (byte)cmd.AddressRegister;
            buf[4] = (byte)(cmd.DataReg.Length >> 8);
            buf[5] = (byte)cmd.DataReg.Length;
            buf[6] = (byte)no_of_byte;
            int index = 7;
            for (int i = 0; i < no_of_register; i++)
            {
                buf[index] = (byte)(cmd.DataReg[i] >> 8);
                index++;
                buf[index] = (byte)cmd.DataReg[i];
                index++;
            }
            int crc = ModbusUtils.CalculateCRC(buf, frame_size - 2);
            buf[frame_size - 2] = (byte)(crc >> 8);
            buf[frame_size - 1] = (byte)crc;
            return buf;
        }
        private byte[] ReadWriteRegister_CreateMessage(ReadWriteRegisterRequest request)
        {
            ReadWriteRegisterRequest cmd = request;
            int no_of_byte = cmd.DataRegisterWrite.Length * 2;
            int no_of_register = cmd.DataRegisterWrite.Length;
            int frame_size = 13 + no_of_byte;
            byte[] buf = new byte[frame_size];
            int idx = 0;
            buf[idx++] = cmd.SlaverAddress;
            buf[idx++] = cmd.FuntionCode;
            // read register 
            buf[idx++] = (byte)(cmd.ReadAddress >> 8);
            buf[idx++] = (byte)cmd.ReadAddress;
            buf[idx++] = (byte)(cmd.NumberReadRegister >> 8);
            buf[idx++] = (byte)(cmd.NumberReadRegister);
            // write register
            buf[idx++] = (byte)(cmd.WriteAddress >> 8);
            buf[idx++] = (byte)(cmd.WriteAddress);
            buf[idx++] = (byte)(cmd.DataRegisterWrite.Length >> 8);
            buf[idx++] = (byte)(cmd.DataRegisterWrite.Length);
            buf[idx++] = (byte)no_of_byte;

            for (int i = 0; i < no_of_register; i++)
            {
                buf[idx++] = (byte)(cmd.DataRegisterWrite[i] >> 8);
                buf[idx++] = (byte)cmd.DataRegisterWrite[i];
            }
            int crc = ModbusUtils.CalculateCRC(buf, frame_size - 2);
            buf[frame_size - 2] = (byte)(crc >> 8);
            buf[frame_size - 1] = (byte)crc;

            return buf;
        }

        #endregion

        #region response
        private ReadCoilStatusResponse ReadCoilStatus_DecodeMessage(byte[] frameResponse)
        {
            ReadCoilStatusResponse msg = new ReadCoilStatusResponse();
            int byte_count = frameResponse[2];// byte count 
            int no_of_register = byte_count / 2 + byte_count % 2;
            UInt16[] register = new UInt16[no_of_register];
            int index = 3, byte_process = 0;
            for (int i = 0; i < no_of_register; i++)
            {
                register[i] |= (UInt16)(((int)frameResponse[index]) << 8);
                byte_process++;
                if (byte_process < byte_count)
                {
                    register[i] |= (UInt16)(((int)frameResponse[index + 1]));
                    byte_process++;
                    index += 2;
                }
            }
            msg.SlaverAddress = frameResponse[0];
            msg.Register = register;
            return msg;
        }
        private ReadInputStatusResponse ReadInputStatus_DecodeMessage(byte[] frameResponse)
        {
            ReadInputStatusResponse msg = new ReadInputStatusResponse();
            int byte_count = frameResponse[2];// byte count 
            int no_of_register = byte_count / 2 + byte_count % 2;
            UInt16[] register = new UInt16[no_of_register];
            int index = 3, byte_process = 0;
            for (int i = 0; i < no_of_register; i++)
            {
                register[i] |= (UInt16)(((int)frameResponse[index]) << 8);
                byte_process++;
                if (byte_process < byte_count)
                {
                    register[i] |= (UInt16)(((int)frameResponse[index + 1]));
                    byte_process++;
                    index += 2;
                }
            }
            msg.SlaverAddress = frameResponse[0];
            msg.Register = register;
            return msg;
        }
        private ReadHoldingRegisterResponse ReadHoldingRegister_DecodeMessage(byte[] buf)
        {
            ReadHoldingRegisterResponse msg = new ReadHoldingRegisterResponse();
            int lenBuffer = buf.Length;
            int byte_count = buf[2];
            int no_of_register = byte_count / 2;
            UInt16[] register = new UInt16[no_of_register];
            int index = 3;
            for (int i = 0; i < no_of_register; i++)
            {
                register[i] = (UInt16)(((int)buf[index]) << 8);// | (UInt16)(((int)buf[index + 1]));
                register[i] |= (UInt16)(((int)buf[index + 1]));
                index += 2;
            }
            msg.SlaverAddress = buf[0];
            msg.Register = register;
            return msg;
        }
        private ReadInputRegisterResponse ReadInputRegister_DecodeMessage(byte[] buf)
        {
            ReadInputRegisterResponse msg = new ReadInputRegisterResponse();
            int lenBuffer = buf.Length;
            int byte_count = buf[2];
            int no_of_register = byte_count / 2;
            UInt16[] register = new UInt16[no_of_register];
            int index = 3;
            for (int i = 0; i < no_of_register; i++)
            {
                register[i] = (UInt16)(((int)buf[index]) << 8);// | (UInt16)(((int)buf[index + 1]));
                register[i] |= (UInt16)(((int)buf[index + 1]));
                index += 2;
            }
            msg.SlaverAddress = buf[0];
            msg.Register = register;
            return msg;
        }
        private ForceMutipleCoilsResponse ForceMutipleCoils_DecodeMessage(byte[] frameResponse)
        {
            ForceMutipleCoilsResponse respone = new ForceMutipleCoilsResponse();
            respone.SlaverAddress = frameResponse[0];
            respone.CoidAddress = BitConverter.ToUInt16(frameResponse, 2);
            respone.QuantityCoil = BitConverter.ToUInt16(frameResponse, 4);
            return respone;
        }
        private PresetMutipleRegisterResponse PresetMutipleRegister_DecodeMessage(byte[] frameResponse)
        {
            PresetMutipleRegisterResponse respone = new PresetMutipleRegisterResponse();
            respone.SlaverAddress = frameResponse[0];
            respone.AddressRegister = BitConverter.ToUInt16(frameResponse, 2);
            respone.NoOfRegister = BitConverter.ToUInt16(frameResponse, 4);
            return respone;
        }
        private PresetSingleRegisterResponse PresetSingleRegister_DecodeMessage(byte[] frameResponse)
        {
            PresetSingleRegisterResponse response = new PresetSingleRegisterResponse();
            response.SlaverAddress = frameResponse[0];
            response.AddressRegister = BitConverter.ToUInt16(frameResponse, 2);
            response.Data = BitConverter.ToUInt16(frameResponse, 4);
            return response;

        }
        private ReadWriteRegisterResponse ReadWriteRegister_DecodeMessage(byte[] buf)
        {
            ReadWriteRegisterResponse msg = new ReadWriteRegisterResponse();
            int lenBuffer = buf.Length;
            int byte_count = buf[2];
            int no_of_register = byte_count / 2;
            UInt16[] register = new UInt16[no_of_register];
            int index = 3;
            for (int i = 0; i < no_of_register; i++)
            {
                register[i] = (UInt16)(((int)buf[index]) << 8);// 
                register[i] |= (UInt16)(((int)buf[index + 1]));
                index += 2;
            }
            msg.SlaverAddress = buf[0];
            msg.Register = register;
            return msg;
        }

        #endregion
    }
}
