using KModbus.Data.Options;
using KModbus.Formatter;
using KModbus.Message;
using KModbus.Message.Handles;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KModbus.IO
{
    // phụ trách gửi nhận gói tin modbus 
    public class ModbusTcpClientChannel
    {
        private IModbusChannel _channel;
        readonly AsyncLock _mutex = new AsyncLock();
        ModbusTcpFormatter _tcpFormater;
        ModbusRtuFormatter _rtuFormater;
        private ushort _transactionId = 0;
        private EModbusPacketProtocal _protocal;

        public ModbusTcpClientChannel(IModbusChannel channel,ushort transactionId)
        {
            _channel = channel;
            _transactionId= transactionId;
            _protocal = EModbusPacketProtocal.TcpIp;
            _tcpFormater = new ModbusTcpFormatter();
        }

        public ModbusTcpClientChannel(IModbusChannel channel)
        {
            _channel = channel;
            _protocal = EModbusPacketProtocal.RtuOverTcpIp;
            _rtuFormater = new ModbusRtuFormatter();
        }

        public async Task ConnectAsync(CancellationToken c)
        {
            await _channel.ConnectAsync(c).ConfigureAwait(false);
        }

        public async Task SendMessageAync(IModbusRequest request,CancellationToken c)
        {
            using (await _mutex.LockAsync(c).ConfigureAwait(false))
            {
                if (_protocal == EModbusPacketProtocal.TcpIp)
                {
                    var frame = _tcpFormater.Create(_transactionId, request);
                    await _channel.WriteAsync(frame, 0, frame.Length, c).ConfigureAwait(false);
                }
                else if(_protocal==EModbusPacketProtocal.RtuOverTcpIp)
                {
                    var frame = _rtuFormater.Create(request);
                    await _channel.WriteAsync(frame, 0, frame.Length, c).ConfigureAwait(false);
                }
            }
        }

        public async Task<IModbusResponse> ReceieveAsync(CancellationToken c)
        {
            while (!c.IsCancellationRequested)
            {
                if(_protocal==EModbusPacketProtocal.TcpIp)
                {
                    byte[] frameStart = new byte[6];
                    if(await _channel.ReadAsync(frameStart, 0, frameStart.Length, c).ConfigureAwait(false) == frameStart.Length)
                    {
                        ushort len = (ushort)(((int)frameStart[4]) << 8 | frameStart[5]);
                        if (len > 256 - frameStart.Length)
                            throw new Exception("tcp data len too long");
                        byte[] frameEnd = new byte[len];
                        if (await _channel.ReadAsync(frameEnd, 0, frameEnd.Length, c) == frameEnd.Length)
                        {
                            byte[] frame = MessageHandle_Service.ConcatFrame(frameStart, frameEnd);
                            var result=_tcpFormater.DecodeMessage(frame);
                            if (result.Item2 == _transactionId)
                            {
                                return result.Item1;
                            }
                            else
                            {
                                throw new Exception("transaction id is not equal");
                            }    
                        }
                    }
                    return null;
                }
                else // frame rtu
                {
                    byte[] frameStart =new byte[4];
                    if(await _channel.ReadAsync(frameStart, 0, frameStart.Length, c).ConfigureAwait(false) == frameStart.Length)
                    {
                        byte[] frameEnd = new byte[MessageHandle_Service.GetRtuRequestBytesToRead(frameStart)];
                        if(await _channel.ReadAsync(frameEnd, 0, frameEnd.Length, c).ConfigureAwait(false) == frameEnd.Length)
                        {
                            byte[] frame = MessageHandle_Service.ConcatFrame(frameStart, frameEnd);
                            // đã đủ dữ liệu -> tiến hành decode
                            var result = _rtuFormater.DecodeMessage(frame);
                            return result;
                        }
                    }
                    return null;
                }
            }
            return null;
        }

        public void DisconnectAsync()
        {
             _channel.DisconnectAsync();
        }

    }
}
