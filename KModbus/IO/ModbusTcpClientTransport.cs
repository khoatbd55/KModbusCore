using KModbus.Data.Options;
using KModbus.Ex;
using KModbus.Formatter;
using KModbus.Interfaces;
using KModbus.Message;
using KUtilities.TaskExtentions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KModbus.IO
{
    public class ModbusTcpClientTransport: IMobusTransportAdapter
    {
        public event ExceptionOccurEventHandle OnExceptionOccur;
        // event for Message Modbus Recieved
        public event MessageModbusEventHandle MessageRecieved;
        // event for Connection closed
        public event ConnectionCloseEventHandle Closed;

        // serial comprot
        protected ModbusTcpClientChannel _clientChannel;
        // connection is closing due to peer
        public const int ResponseFrameStartLength = 4;
        private CancellationTokenSource _backgroundCancellationSource = new CancellationTokenSource();
        private CancellationTokenSource _readCancellationSource = new CancellationTokenSource();

        Task _taskRecv;
        Task _taskStop;
        Task _taskSendData;
        KAsyncQueue<Exception> _stopQueue = new KAsyncQueue<Exception>();
        object _lockStop = new object();
        KAsyncQueue<MsgComportRecv> _recvQueue = new KAsyncQueue<MsgComportRecv>();
        KAsyncQueue<IModbusRequest> _sendQueue = new KAsyncQueue<IModbusRequest>();
        ModbusClientTcpOptions _option = new ModbusClientTcpOptions();
        readonly object _syncLastTimeConnect = new object();
        DateTime _lastTimeConnect;
        Task _taskKeepAlive;
        public ModbusTcpClientTransport(ModbusClientTcpOptions config)
        {
            _option = config;
        }

        protected void OnMessageRecieve(IModbusResponse msg)
        {
            if (this.MessageRecieved != null)
            {
                this.MessageRecieved(this, msg);
            }
        }

        protected void EventExceptionOccur(Exception ex)
        {
            if (OnExceptionOccur != null)
            {
                this.OnExceptionOccur(this, ex);
            }
        }
        /// <summary>
        /// Wrapper method for Closing connection
        /// </summary>
        protected void OnConnectionClosing(Exception e)
        {
            lock (_lockStop)
            {
                if (!_backgroundCancellationSource.Token.IsCancellationRequested)
                {
                    _stopQueue.Enqueue(e);
                }
            }
        }
        public void Disconnect()
        {
            this.OnConnectionClosing(new Exception("disconnect by require"));
        }

        public async Task DisconnectAsync()
        {
            this.OnConnectionClosing(new Exception("disconnect by requires"));
            await WaitForTask(_taskStop);
        }

        public async Task ConnectAsync()
        {
            _backgroundCancellationSource = new CancellationTokenSource();
            var c = _backgroundCancellationSource.Token;
            ModbusTcpChannel _tcpChannel = new ModbusTcpChannel(_option.TcpOption);
            if(_option.PacketProtocal==EModbusPacketProtocal.TcpIp)
                _clientChannel = new ModbusTcpClientChannel(_tcpChannel, _option.TransactionId);
            else
                _clientChannel = new ModbusTcpClientChannel(_tcpChannel);
            await _clientChannel.ConnectAsync(c).ConfigureAwait(false);
            _recvQueue = new KAsyncQueue<MsgComportRecv>();
            _sendQueue = new KAsyncQueue<IModbusRequest>();
            _readCancellationSource = new CancellationTokenSource();
            _stopQueue = new KAsyncQueue<Exception>();
            _taskRecv = Task.Run(() => ProcessInflightRecieved(c, _readCancellationSource.Token), c);
            _taskSendData = Task.Run(() => ProcessSendData(c), c);
            _taskStop = Task.Run(() => ProcessStopAllTask(c), c);
            _lastTimeConnect = DateTime.Now;
            _taskKeepAlive = Task.Run(() => ProcessCheckTimeOut(c), c);
           
        }

        public Task SendDataAsync(IModbusRequest request)
        {
            SendData(request);
            return Task.CompletedTask;
        }

        private async Task ProcessCheckTimeOut(CancellationToken c)
        {
            while (!c.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, c).ConfigureAwait(false);
                    lock (_syncLastTimeConnect)
                    {
                        if (DateTime.Now.Subtract(_lastTimeConnect).TotalSeconds >= _option.TimeOutConnect)
                        {
                            this.OnConnectionClosing(new Exception("time out connection"));
                        }
                    }

                }
                catch (Exception)
                {

                }

            }
        }

        private async Task ProcessSendData(CancellationToken c)
        {
            while (!c.IsCancellationRequested)
            {
                try
                {
                    var itemQueue = await _sendQueue.TryDequeueAsync(c).ConfigureAwait(false);
                    
                    if (!c.IsCancellationRequested && itemQueue.IsSuccess)
                    {
                        lock (_syncLastTimeConnect)
                        {
                            _lastTimeConnect = DateTime.Now;
                        }    
                        await _clientChannel.SendMessageAync(itemQueue.Item, c).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    this.OnConnectionClosing(ex);
                }
            }

        }

        public void SendData(IModbusRequest request)
        {
            _sendQueue.Enqueue(request);
        }

        private async Task ProcessStopAllTask(CancellationToken c)
        {
            try
            {
                while (!c.IsCancellationRequested)
                {
                    var stop = await _stopQueue.TryDequeueAsync(c).ConfigureAwait(false);
                    if (stop.IsSuccess)
                    {
                        await CloseCoreAsync(stop.Item).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception)
            {
                //_logger.LogError("{time} {0} cache service - fail in stop task {1}", DateTime.Now, this._name, e.Message);
            }
        }

        public async Task CloseCoreAsync(Exception ex)
        {
            lock (_lockStop)
            {
                _backgroundCancellationSource?.Cancel();
            }
            try
            {
                if (_clientChannel != null)
                    _clientChannel.DisconnectAsync();
            }
            catch (Exception)
            {

            }
            await WaitForTask(_taskRecv);
            await WaitForTask(_taskSendData);
            await WaitForTask(_taskKeepAlive);
            _recvQueue.Clear();
            _ = Task.Run(() =>
            {
                // run in task avoid deadlook
                if (Closed != null)
                    Closed.Invoke(this, new EventArgs());
            });

        }


        protected async Task ProcessInflightRecieved(CancellationToken c, CancellationToken cRead)
        {
            try
            {
                while (!c.IsCancellationRequested)
                {
                    var msgRespond = await _clientChannel.ReceieveAsync(c).ConfigureAwait(false);
                    if (msgRespond != null)
                    {
                        this.OnMessageRecieve(msgRespond);
                    }
                    else
                    {
                        throw new FrameModbusDecodeException("message modbus decode error");
                    }
                }
            }
            catch (Exception ex)
            {
                this.EventExceptionOccur(ex);
                this.OnConnectionClosing(ex);
            }
        }


        private async Task WaitForTask(Task task)
        {
            try
            {
                if (task != null && !task.IsCompleted)
                    await task.ConfigureAwait(false);
            }
            catch (Exception)
            {

            }
        }

    }
}
