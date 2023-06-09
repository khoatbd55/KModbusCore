using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Message.Handles
{
    public class MessageCompareModbus
    {
        public static bool Compare(IModbusRequest iRequest, IModbusRequest iResponse)
        {
            switch(iRequest.FuntionCode)
            {
                case ModbusFunctionCodes.ReadCoilStatus:
                    {
                        if(iResponse is ReadCoilStatusRequest)
                        {
                            var request = (ReadCoilStatusRequest)iRequest;
                            var response = (ReadCoilStatusRequest)iResponse;
                            if (request.NoOfPoints == response.NoOfPoints 
                                && request.AddressCoil == response.AddressCoil )
                            {
                                return true;
                            }    
                        }    
                    }
                    break;
                case ModbusFunctionCodes.ReadInputStauts:
                    {
                        if(iResponse is ReadInputStatusRequest)
                        {
                            var request = (ReadInputStatusRequest)iRequest;
                            var response = (ReadInputStatusRequest)iResponse;
                            if(request.AddressCoil==response.AddressCoil &&
                                request.NoOfPoints==response.NoOfPoints)
                            {
                                return true;
                            }    
                        }    
                    }
                    break;
                case ModbusFunctionCodes.ReadHoldingRegisters:
                    {
                        if(iResponse is ReadHoldingRegisterRequest)
                        {
                            var request = (ReadHoldingRegisterRequest)iRequest;
                            var response = (ReadHoldingRegisterRequest)iResponse;
                            if(request.AddressRegister == response.AddressRegister &&
                                request.NoOfPoints==response.NoOfPoints)
                            {
                                return true;
                            }    
                        }    
                    }
                    break;
                case ModbusFunctionCodes.ReadInputRegisters:
                    {
                        if(iResponse is ReadInputRegisterRequest)
                        {
                            var request = (ReadInputRegisterRequest)iRequest;
                            var response = (ReadInputRegisterRequest)iResponse;
                            if (request.AddressRegister == response.AddressRegister &&
                                request.NoOfPoints == response.NoOfPoints)
                            {
                                return true;
                            }
                        }    
                    }
                    break;
                case ModbusFunctionCodes.WriteSingleCoil:
                    {
                        
                    }
                    break;
                case ModbusFunctionCodes.PresetSingleRegister:
                    {
                        if(iResponse is PresetSingleRegisterRequest)
                        {
                            var request = (PresetSingleRegisterRequest)iRequest;
                            var response = (PresetSingleRegisterRequest)iResponse;
                            if(request.RegisterAddress==response.RegisterAddress)
                            {
                                return true;
                            }    
                        }    
                    }
                    break;
                case ModbusFunctionCodes.ForceMutiplsCoils:
                    break;
                case ModbusFunctionCodes.PresetMutipleRegister:
                    {
                        if(iResponse is PresetMutipleRegisterRequest)
                        {
                            var request = (PresetMutipleRegisterRequest)iRequest;
                            var response = (PresetMutipleRegisterRequest)iResponse;
                            if(request.AddressRegister==response.AddressRegister &&
                                request.DataReg.Length==response.DataReg.Length)
                            {
                                return true;
                            }    
                        }    
                    }
                    break;
                case ModbusFunctionCodes.WriteGenernal:
                    break;
                case ModbusFunctionCodes.ReadWriteRegister:
                    {
                        if (iResponse is ReadWriteRegisterRequest)
                        {
                            var request = (ReadWriteRegisterRequest)iRequest;
                            var response = (ReadWriteRegisterRequest)iResponse;
                            if (request.ReadAddress == response.ReadAddress &&
                                request.NumberReadRegister == response.NumberReadRegister && 
                                request.WriteAddress==response.WriteAddress)
                            {
                                return true;
                            }
                        }
                    }
                    break;

            }
            return false;
        }
    }
}
