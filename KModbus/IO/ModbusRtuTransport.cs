using KModbus.Ex;
using KModbus.Extention;
using KModbus.Formatter;
using KModbus.Message;
using KModbus.Message.Handles;
using KUtilities.TaskExtentions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KModbus.IO
{
    internal class ModbusRtuTransport
    {

        // Delegate for MessageModbus Event Handle
        public delegate void MessageModbusEventHandle(object sender, IModbusResponse e);
        // Delegate for connection close
        public delegate void ConnectionCloseEventHandle(object sender, EventArgs e);
        //
        public delegate void ExceptionOccurEventHandle(object sender, Exception ex);

        public event ExceptionOccurEventHandle OnExceptionOccur;
        // event for Message Modbus Recieved
        public event MessageModbusEventHandle MessageRecieved;
        // event for Connection closed
        public event ConnectionCloseEventHandle Closed;

        readonly KAsyncLock _syncRoot = new KAsyncLock();
        // serial comprot
        protected SerialPort comport;
        // connection is closing due to peer
        public const int ResponseFrameStartLength = 4;
        private CancellationTokenSource _backgroundCancellationSource = new CancellationTokenSource();
        private CancellationTokenSource _readCancellationSource = new CancellationTokenSource();

        private IModbusFormatter _modbusFormatter;
        Task _taskRecv;
        Task _taskStop;
        Task _taskSendData;
        KAsyncQueue<Exception> _stopQueue = new KAsyncQueue<Exception>();
        object _lockStop = new object();
        KAsyncQueue<MsgComportRecv> _recvQueue = new KAsyncQueue<MsgComportRecv>();
        KAsyncQueue<byte[]> _sendQueue = new KAsyncQueue<byte[]>();

        public ModbusRtuTransport(IModbusFormatter modbusFormatter)
        {
            _modbusFormatter = modbusFormatter;
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

        public void Open(int baudRate, string portName, Parity parity, int databit, StopBits stopBit)
        {
            try
            {
                comport = new SerialPort();
                comport.BaudRate = baudRate;
                comport.PortName = portName;
                comport.Parity = parity;
                comport.DataBits = databit;
                comport.StopBits = stopBit;
                comport.Open();
            }
            catch (Exception ex)
            {
                comport.Dispose();
                throw new Exception(ex.Message, ex.InnerException);
            }
            _recvQueue = new KAsyncQueue<MsgComportRecv>();
            _sendQueue = new KAsyncQueue<byte[]>();
            _readCancellationSource = new CancellationTokenSource();
            _backgroundCancellationSource = new CancellationTokenSource();
            var c = _backgroundCancellationSource.Token;
            _stopQueue = new KAsyncQueue<Exception>();
            _taskRecv = Task.Run(() => ProcessInflightRecieved(c, _readCancellationSource.Token), c);
            _taskSendData = Task.Run(() => ProcessSendData(c), c);
            _taskStop = Task.Run(() => ProcessStopAllTask(c), c);
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
                        await comport.BaseStream.WriteAsync(itemQueue.Item, 0, itemQueue.Item.Length, c).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    this.OnConnectionClosing(ex);
                }
            }

        }

        public void Open(string portName, int baud)
        {
            Open(baud, portName, Parity.None, 8, StopBits.One);
        }

        public void Open(string portName)
        {
            Open(9600, portName, Parity.None, 8, StopBits.One);
        }

        public void SendData(byte[] buf)
        {
            _sendQueue.Enqueue(buf);
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
            _ = Task.Run(() =>
            {
                // fix deadlock 
                try
                {
                    comport.DtrEnable = false;
                    comport.RtsEnable = false;
                    comport.DiscardInBuffer();
                    comport.DiscardOutBuffer();
                    this.comport.Close();
                }
                catch (Exception)
                {

                }
            });
            await Task.Delay(2000).ConfigureAwait(false);// đợi cho cổng đóng hẳn
            _recvQueue.Clear();
            _ = Task.Run(() =>
            {
                // run in task avoid deadlook
                if (Closed != null)
                    Closed.Invoke(this, new EventArgs());
            });

        }
        protected void EnqueueMsgRecComport(MsgComportRecv msg)
        {
            _recvQueue.Enqueue(msg);
        }
        protected async Task ProcessInflightRecieved(CancellationToken c, CancellationToken cRead)
        {
            try
            {
                while (!c.IsCancellationRequested)
                {
                    byte[] frameStart = await ReadAsync(ResponseFrameStartLength, c).ConfigureAwait(false);
                    if (frameStart.Length == 4)
                    {
                        var byteToRead = MessageHandle_Service.GetRtuRequestBytesToRead(frameStart);
                        if (byteToRead > 0 && byteToRead < 255)
                        {
                            byte[] frameEnd = await ReadAsync(byteToRead, c).ConfigureAwait(false);
                            byte[] frame = MessageHandle_Service.ConcatFrame(frameStart, frameEnd);
                            try
                            {
                                var msgRespond = _modbusFormatter.DecodeMessage(frame);// dữ liệu phản hồi
                                this.OnMessageRecieve(msgRespond);
                            }
                            catch (Exception ex)
                            {
                                throw new FrameModbusDecodeException(ex.Message, ex);
                            }
                        }
                        else
                        {
                            throw new FrameModbusRecieveException("frame modbus not correct,body frame difference 0 and 255");
                        }
                    }
                    else
                    {
                        throw new FrameModbusRecieveException("frame modbus not correct ,header < 4 bytes");
                    }
                }
            }
            catch (Exception ex)
            {
                this.EventExceptionOccur(ex);
                this.OnConnectionClosing(ex);
            }
        }

        public async Task<byte[]> ReadAsync(int count, CancellationToken c)
        {
            byte[] frameBytes = new byte[count];
            int numBytesRead = 0;
            while (numBytesRead != count && !c.IsCancellationRequested)
            {
                var result = await comport.BaseStream.ReadAsync(frameBytes, numBytesRead, count - numBytesRead, c).ConfigureAwait(false);
                if (result == 0)
                    break;
                else
                {
                    numBytesRead += result;
                }
            }
            return frameBytes;
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
