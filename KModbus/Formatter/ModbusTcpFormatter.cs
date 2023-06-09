using KModbus.Interfaces;
using KModbus.Message;
using KModbus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Formatter
{
    internal class ModbusTcpFormatter 
    {
        private const int MIN_LEN_FRAME = 11;
        public byte[] Create( ushort transactionId, IModbusRequest request)
        {
            if(request is ReadCoilStatusRequest readCoil)
            {
                return ReadCoilStatus_CreateMessage(transactionId,readCoil);
            }
            else if(request is ReadInputStatusRequest readInputs)
            {
                return ReadInputStatus_CreateMessage(transactionId,readInputs);
            }    
            else if(request is ReadHoldingRegisterRequest readHoldingRegister)
            {
                return ReadHoldingRegister_CreateMessage(transactionId,readHoldingRegister);
            }
            else if(request is ReadInputRegisterRequest readInputRegister)
            {
                return ReadInputRegister_CreateMessage(transactionId,readInputRegister);
            }
            else if(request is ForceMutipleCoilsRequest forceMutipleCoils)
            {
                return ForceMutipleCoils_CreateMessage(transactionId,forceMutipleCoils);
            }
            else if(request is PresetSingleRegisterRequest presetSingleRegister)
            {
                return PresetSingleRegister_CreateMessage(transactionId,presetSingleRegister);
            }
            else if(request is PresetMutipleRegisterRequest presetMultipleRegister)
            {
                return PresetMultipleRegister_CreateMessage(transactionId,presetMultipleRegister);
            }
            else if(request is ReadWriteRegisterRequest readWriteRegister)
            {
                return ReadWriteRegister_CreateMessage(transactionId, readWriteRegister);
            }
            return null;
        }

        public Tuple<IModbusResponse, ushort> DecodeMessage(byte[] frame)
        {
            // kiểm tra chiều dài của frame
            if (frame.Length < MIN_LEN_FRAME)
                throw new Exception("frame response too short");
            // kiểm tra crc 
            byte functionCode = frame[7];
            if (functionCode > 128)
                throw new Exception("frame response error format");
            else
            {
                ushort transactionId = (ushort)((ushort)(frame[0]) << 8 | (frame[1]));
                var frameResponse = new byte[frame.Length - 6];
                Array.Copy(frame, 6, frameResponse, 0, frameResponse.Length);
                if (functionCode == ModbusFunctionCodes.ReadCoilStatus)
                    return new Tuple<IModbusResponse, ushort>(ReadCoilStatus_DecodeMessage(frameResponse), transactionId);
                else if (functionCode == ModbusFunctionCodes.ReadInputStauts)
                    return new Tuple<IModbusResponse, ushort>(ReadInputStatus_DecodeMessage(frameResponse), transactionId);
                else if (functionCode == ModbusFunctionCodes.ReadHoldingRegisters)
                    return new Tuple<IModbusResponse, ushort>(ReadHoldingRegister_DecodeMessage(frameResponse), transactionId);
                else if (functionCode == ModbusFunctionCodes.ReadInputRegisters)
                    return new Tuple<IModbusResponse, ushort>(ReadInputRegister_DecodeMessage(frameResponse), transactionId);
                else if (functionCode == ModbusFunctionCodes.PresetSingleRegister)
                    return new Tuple<IModbusResponse, ushort>(PresetSingleRegister_DecodeMessage(frameResponse), transactionId);
                else if (functionCode == ModbusFunctionCodes.ForceMutiplsCoils)
                    return new Tuple<IModbusResponse, ushort>(ForceMutipleCoils_DecodeMessage(frameResponse), transactionId);
                else if (functionCode == ModbusFunctionCodes.PresetMutipleRegister)
                    return new Tuple<IModbusResponse, ushort>(PresetMutipleRegister_DecodeMessage(frameResponse), transactionId);
                else if (functionCode == ModbusFunctionCodes.ReadWriteRegister)
                    return new Tuple<IModbusResponse, ushort>(ReadWriteRegister_DecodeMessage(frameResponse), transactionId);
                else
                    throw new Exception("function modbus not correct");
            }
        }

        #region request
        private byte[] ReadCoilStatus_CreateMessage(ushort transactionId, ReadCoilStatusRequest request)
        {
            return CreateReadHeader(transactionId, request.SlaverAddress,
                            request.FuntionCode, request.AddressCoil, request.NoOfPoints);
        }
        private byte[] ReadInputStatus_CreateMessage(ushort transactionId, ReadInputStatusRequest request)
        {
            return CreateReadHeader( transactionId, request.SlaverAddress,
                            request.FuntionCode, request.AddressCoil, request.NoOfPoints);
        }
        private byte[] ReadHoldingRegister_CreateMessage(ushort transactionId,ReadHoldingRegisterRequest request)
        {
            return CreateReadHeader(transactionId, request.SlaverAddress, 
                            request.FuntionCode, request.AddressRegister, request.NoOfPoints);
        }
        private byte[] ReadInputRegister_CreateMessage(ushort transactionId,ReadInputRegisterRequest request)
        {
            return CreateReadHeader(transactionId, request.SlaverAddress, 
                            request.FuntionCode, request.AddressRegister, request.NoOfPoints);
        }
        private byte[] ForceMutipleCoils_CreateMessage(ushort transactionId,ForceMutipleCoilsRequest request)
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
            int buffer_size = 13 + no_of_byte;
            byte[] buf = new byte[buffer_size];

            byte[] _id = BitConverter.GetBytes((short)transactionId);
            buf[0] = _id[1];				// Slave id high byte
            buf[1] = _id[0];				// Slave id low byte
            byte[] _size = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)(6 + no_of_byte)));
            buf[4] = _size[0];				// Complete message size in bytes
            buf[5] = _size[1];				// Complete message size in bytes
            buf[6] = cmd.SlaverAddress;				// Slave address
            buf[7] = cmd.FuntionCode;				// Function code
            byte[] _adr = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)cmd.CoilAddress));
            buf[8] = _adr[0];				// Start address
            buf[9] = _adr[1];				// Start address
            byte[] _cnt = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)cmd.QuantityColis));
            buf[10] = _cnt[0];             // Number of bytes
            buf[11] = _cnt[1];             // Number of bytes
            buf[12] = (byte)no_of_byte;
            int idx = 13;
            for (int i = 0; i < no_of_byte; i++)
            {
                buf[idx++] = (byte)(cmd.CoilData[i] >> 8);
                buf[idx++] = (byte)(cmd.CoilData[i]);
            }
            return buf;
        }
        private byte[] PresetSingleRegister_CreateMessage(ushort transactionId,PresetSingleRegisterRequest request)
        {
            return CreateReadHeader(transactionId, request.SlaverAddress, request.FuntionCode, request.RegisterAddress, request.PresetData);
        }
        private byte[] PresetMultipleRegister_CreateMessage(ushort transactionId,PresetMutipleRegisterRequest request)
        {
            PresetMutipleRegisterRequest cmd = request;
            int no_of_byte = cmd.DataReg.Length * 2;
            int no_of_register = cmd.DataReg.Length;
            int frame_size = 13 + no_of_byte;
            byte[] buf = new byte[frame_size];

            byte[] _id = BitConverter.GetBytes((short)transactionId);
            buf[0] = _id[1];				// Slave id high byte
            buf[1] = _id[0];				// Slave id low byte
            byte[] _size = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)(7 + no_of_byte)));
            buf[4] = _size[0];				// Complete message size in bytes
            buf[5] = _size[1];				// Complete message size in bytes
            buf[6] = cmd.SlaverAddress;				// Slave address
            buf[7] = cmd.FuntionCode;				// Function code
            byte[] _adr = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)cmd.AddressRegister));
            buf[8] = _adr[0];				// Start address
            buf[9] = _adr[1];				// Start address
            byte[] _cnt = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)cmd.DataReg.Length));
            buf[10] = _cnt[0];             // Number of bytes
            buf[11] = _cnt[1];             // Number of bytes
            buf[12] = (byte)no_of_byte;
            int idx = 13;
            for (int i = 0; i < no_of_register; i++)
            {
                buf[idx++] = (byte)(cmd.DataReg[i] >> 8);
                buf[idx++] = (byte)(cmd.DataReg[i]);
            }
            return buf;
        }
        private byte[] ReadWriteRegister_CreateMessage(ushort transactionId,ReadWriteRegisterRequest request)
        {
            ReadWriteRegisterRequest cmd = request;
            int no_of_byte = cmd.DataRegisterWrite.Length * 2;
            int no_of_register = cmd.DataRegisterWrite.Length;
            int frame_size = 17 + no_of_byte;
            byte[] buf = new byte[frame_size];
            byte[] _id = BitConverter.GetBytes((short)transactionId);
            buf[0] = _id[1];						// Slave id high byte
            buf[1] = _id[0];						// Slave id low byte
            byte[] _size = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)(no_of_byte+10)));
            buf[4] = _size[0];						// Complete message size in bytes
            buf[5] = _size[1];						// Complete message size in bytes
            buf[6] = cmd.SlaverAddress;							// Slave address
            buf[7] = cmd.FuntionCode;	// Function code
            byte[] _adr_read = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)cmd.ReadAddress));
            buf[8] = _adr_read[0];					// Start read address
            buf[9] = _adr_read[1];					// Start read address
            byte[] _cnt_read = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)cmd.NumberReadRegister));
            buf[10] = _cnt_read[0];				// Number of bytes to read
            buf[11] = _cnt_read[1];				// Number of bytes to read
            byte[] _adr_write = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)cmd.WriteAddress));
            buf[12] = _adr_write[0];				// Start write address
            buf[13] = _adr_write[1];				// Start write address
            byte[] _cnt_write = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)cmd.DataRegisterWrite.Length));
            buf[14] = _cnt_write[0];				// Number of bytes to write
            buf[15] = _cnt_write[1];				// Number of bytes to write
            buf[16] = (byte)no_of_byte;
            int idx = 17;
            for (int i = 0; i < no_of_register; i++)
            {
                buf[idx++] = (byte)(cmd.DataRegisterWrite[i] >> 8);
                buf[idx++] = (byte)cmd.DataRegisterWrite[i];
            }
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

        private byte[] CreateReadHeader(ushort transactionId, byte slaveId, byte function, ushort startAddress, ushort length) 
        {
            byte[] data = new byte[12];

            byte[] _id = BitConverter.GetBytes((short)transactionId);
            data[0] = _id[1];			    // Slave id high byte
            data[1] = _id[0];				// Slave id low byte
            data[5] = 6;					// Message size
            data[6] = slaveId;					// Slave address
            data[7] = function;				// Function code
            byte[] _adr = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)startAddress));
            data[8] = _adr[0];				// Start address
            data[9] = _adr[1];				// Start address
            byte[] _length = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)length));
            data[10] = _length[0];			// Number of data to read
            data[11] = _length[1];			// Number of data to read
            return data;
        }

    }
}
