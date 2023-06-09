using KModbus.Data;
using KModbus.Data.Options;
using KModbus.Extention;
using KModbus.Formatter;
using KModbus.Interfaces;
using KModbus.IO;
using KModbus.Message;
using KModbus.Message.Handles;
using KModbus.Service.Data;
using KModbus.Service.Event;
using KModbus.Service.Event.Child;
using KModbus.Service.Model;
using KUtilities.TaskExtentions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KModbus.Service.TCP
{
    public class ModbusTcpClient:IModbusMaster
    {

        CancellationTokenSource _backgroundCancelToken;
        Task _taskStop;
        Task _taskReceieve;
        Task _taskSendData;
        Task _taskEvent;
        Task _taskKeepAlive;
        object _lockStop = new object();
        KAsyncQueue<CommandModbus_Service> _commandQueue;
        KAsyncQueue<EventMsgHandle_Base> _eventQueue;
        Queue<IModbusRequest> _rpcCommandQueue;
        int _connectstatus = (int)(EModbusConnectStatus.Closed);

        KAsyncQueue<Exception> _stopQueue;
        ModbusClientOptions _option;
        ModbusTcpClientChannel _clientChannel;
        KAsyncTaskCompletionSource<ModbusCmdResponse_Base<ModbusMessage>> _taskCompleteSouceRpc;
        KAsyncTaskCompletionSource<IModbusResponse> _taskCompleteSouceMessage=new KAsyncTaskCompletionSource<IModbusResponse>();
        readonly object _syncCommand = new object();
        readonly object _syncMessage = new object();
        readonly object _syncWaitHandleSleep = new object();
        readonly object _syncLastTimeConnect = new object();
        DateTime _lastTimeConnect;
        SemaphoreSlim waitHandleSleep;
        int TotalCommandRepeat=0;

        readonly KAsyncEvent<MsgResponseModbus_EventArg> _recievedMessageEvent = new KAsyncEvent<MsgResponseModbus_EventArg>();
        readonly KAsyncEvent<MsgNoResponseModbus_EventArg> _noRespondMessageEvent = new KAsyncEvent<MsgNoResponseModbus_EventArg>();
        readonly KAsyncEvent<MsgClosedConnectionEventArgs> _closedConnectionEvent = new KAsyncEvent<MsgClosedConnectionEventArgs>();
        readonly KAsyncEvent<MsgExceptionEventArgs> _exceptionEvent = new KAsyncEvent<MsgExceptionEventArgs>();

        public event Func<MsgResponseModbus_EventArg, Task> OnRecievedMessageAsync
        {
            add => _recievedMessageEvent.AddHandler(value);
            remove => _recievedMessageEvent.RemoveHandler(value);
        }

        public event Func<MsgNoResponseModbus_EventArg, Task> OnNoRespondMessageAsync
        {
            add => _noRespondMessageEvent.AddHandler(value);
            remove => _noRespondMessageEvent.RemoveHandler(value);
        }

        public event Func<MsgClosedConnectionEventArgs, Task> OnClosedConnectionAsync
        {
            add => _closedConnectionEvent.AddHandler(value);
            remove => _closedConnectionEvent.RemoveHandler(value);
        }

        public event Func<MsgExceptionEventArgs, Task> OnExceptionAsync
        {
            add => _exceptionEvent.AddHandler(value);
            remove => _exceptionEvent.RemoveHandler(value);
        }

        public bool IsRunning
        {
            get
            {
                return ((EModbusConnectStatus)_connectstatus) == EModbusConnectStatus.Opened;
            }
        }

        public ModbusTcpClient()
        {
            
        }

        public async Task ConnectAsync(ModbusClientOptions options)
        {
            this._option = options;
            _backgroundCancelToken = new CancellationTokenSource();
            var c = _backgroundCancelToken.Token;
            TotalCommandRepeat = 0;
            this._commandQueue = new KAsyncQueue<CommandModbus_Service>();
            this._eventQueue = new KAsyncQueue<EventMsgHandle_Base>();
            this._rpcCommandQueue = new Queue<IModbusRequest>();
            this.waitHandleSleep = new SemaphoreSlim(0);
            ModbusTcpChannel _tcpChannel = new ModbusTcpChannel(_option.TcpOption);
            _clientChannel = new ModbusTcpClientChannel(_tcpChannel, _option.TransactionId);
            await _clientChannel.ConnectAsync(c).ConfigureAwait(false);
            _lastTimeConnect = DateTime.Now;
            _stopQueue = new KAsyncQueue<Exception>();
            _taskStop = Task.Run(() => ProcessStopAllTask(c), c);
            _taskReceieve = Task.Run(() => ProcessReceieveData(c), c);
            _taskSendData = Task.Run(() => ProcessInflightCommand(c), c);
            _taskEvent = Task.Run(() => ProcessInflightEvent(c), c);
            _taskKeepAlive = Task.Run(() => ProcessCheckTimeOut(c), c);
            Interlocked.Exchange(ref _connectstatus, (int)EModbusConnectStatus.Opened);
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
                            this.OnClosing(new Exception("time out connection"));
                        }
                    }
                    
                }
                catch (Exception)
                {

                }
                
            }
        }

        private async Task ProcessReceieveData(CancellationToken c)
        {
            try
            {
                while (!c.IsCancellationRequested)
                {
                    var message = await _clientChannel.ReceieveAsync(c).ConfigureAwait(false);
                    if (message != null)
                    {
                        lock (_syncMessage)
                        {
                            _taskCompleteSouceMessage.TrySetResult(message);
                        }
                    }
                    else
                    {
                        throw new Exception("message receive null");
                    }    
                }
            }
            catch (Exception ex)
            {
                this.OnClosing(ex);
            }
            
        }

        private void EnqueueCommand(CommandModbus_Service cmd_data)
        {
            if (cmd_data.Type == CommandModbus_Service.CommandType.Repeat)
            {
                Interlocked.Increment(ref TotalCommandRepeat);
            }
            _commandQueue.Enqueue(cmd_data);
            lock (_syncWaitHandleSleep)
            {
                this.waitHandleSleep.Release();
            }
        }
        private void EnqueueEvent(EventMsgHandle_Base eventData)
        {
            _eventQueue.Enqueue(eventData);
        }
        public void SendCommand_NoRepeat(IModbusRequest requestModbus)
        {
            if (!_backgroundCancelToken.Token.IsCancellationRequested)
            {
                CommandModbus_Service cmd = new CommandModbus_Service(requestModbus, CommandModbus_Service.CommandType.NoRepeat);
                EnqueueCommand(cmd);
            }
        }
        public void SendCommnad_Repeat(IModbusRequest requestModbus)
        {
            if (!_backgroundCancelToken.Token.IsCancellationRequested)
            {
                CommandModbus_Service cmd = new CommandModbus_Service(requestModbus, CommandModbus_Service.CommandType.Repeat);
                EnqueueCommand(cmd);
            }               
        }

        public async Task<ModbusCmdResponse_Base<ModbusMessage>> SendCommandNoRepeatAsync(IModbusRequest request, CancellationToken c)
        {
            if (!_backgroundCancelToken.Token.IsCancellationRequested)
            {
                lock (_rpcCommandQueue)
                {
                    _rpcCommandQueue.Clear();
                    _rpcCommandQueue.Enqueue(request);
                }
                _taskCompleteSouceRpc = new KAsyncTaskCompletionSource<ModbusCmdResponse_Base<ModbusMessage>>();// khởi tạo dữ liệu phản hồi
                SendCommand_NoRepeat(request);
                var ctLink = CancellationTokenSource.CreateLinkedTokenSource(c, _backgroundCancelToken.Token);
                var ct = CancellationTokenSource.CreateLinkedTokenSource(ctLink.Token, new CancellationTokenSource(5000).Token);
                var register = ct.Token.Register(() =>
                {
                    lock (_syncCommand)
                    {
                        _taskCompleteSouceRpc.TrySetCanceled();
                    }
                }, useSynchronizationContext: true);
                Task task;
                lock (_syncCommand)
                {
                    task = _taskCompleteSouceRpc.Task;
                }
                bool isCancellation = true;
                try
                {
                    await task.ConfigureAwait(false);
                    isCancellation = false;
                }
                catch (Exception)
                {
                    isCancellation = true;
                }
                if (isCancellation == false)
                {
                    register.Dispose();
                    return _taskCompleteSouceRpc.Task.Result;
                }
                else
                {
                    ModbusCmdResponse_NoResponse<ModbusMessage> noResponse = new ModbusCmdResponse_NoResponse<ModbusMessage>();
                    noResponse.ResultObj = new ModbusMessage(request, null);
                    return noResponse;
                }
            }
            else
            {
                throw new Exception("Cổng mất kết nối - không thể gửi lệnh ");
            }
        }

        private async Task ProcessInflightCommand(CancellationToken c)
        {
            try
            {
                int totalCommandExcute = 0;
                while (!c.IsCancellationRequested)
                {
                    var queueItem = await _commandQueue.TryDequeueAsync(c);
                    if (queueItem.IsSuccess)
                    {
                        var cmd_data = queueItem.Item;
                        if (cmd_data != null)
                        {
                            bool loop = true;
                            int step = 0;
                            int retry = 0;
                            if (cmd_data.Type == CommandModbus_Service.CommandType.Repeat)
                                totalCommandExcute++;
                            while (loop && !c.IsCancellationRequested)
                            {
                                switch (step)
                                {
                                    case 0:// gửi lệnh
                                        {
                                            _taskCompleteSouceMessage = new KAsyncTaskCompletionSource<IModbusResponse>();
                                            await _clientChannel.SendMessageAync(cmd_data.ModbusRequest, c).ConfigureAwait(false);
                                            var ct = CancellationTokenSource.CreateLinkedTokenSource(c, new CancellationTokenSource(1000).Token);
                                            var register = ct.Token.Register(() =>
                                            {
                                                lock (_syncCommand)
                                                {
                                                    _taskCompleteSouceMessage.TrySetCanceled();
                                                }
                                            }, useSynchronizationContext: false);
                                            Task task;
                                            lock (_syncMessage)
                                            {
                                                task = _taskCompleteSouceMessage.Task;
                                            }
                                            bool isCancellation = true;
                                            try
                                            {
                                                await task.ConfigureAwait(false);
                                                isCancellation = false;
                                            }
                                            catch (Exception)
                                            {
                                                isCancellation = true;
                                            }
                                            if (!isCancellation)// có phản hồi 
                                            {
                                                lock (_syncLastTimeConnect)
                                                {
                                                    _lastTimeConnect = DateTime.Now;
                                                }
                                                register.Dispose();// 
                                                retry = 0;
                                                ModbusMessage msgModbus = new ModbusMessage(cmd_data.ModbusRequest, _taskCompleteSouceMessage.Task.Result);
                                                EnqueueEvent(new EventMsgHandle_Response(msgModbus));
                                                loop = false;
                                                // trễ 1 khoảng thời gian sau khi nhận được phản hổi từ modbus slaver
                                                await Task.Delay(_option.DelayResponse, c).ConfigureAwait(false);
                                            }
                                            else
                                            {
                                                if (++retry >= 2)
                                                {
                                                    step = 2;// không có phản hồi từ dưới thiết bị gửi lên
                                                    EnqueueEvent(new EventMsgHandle_NoResponse(cmd_data.ModbusRequest));
                                                    // xử lí thiết bị không phản hồi
                                                }
                                            }
                                        }
                                        break;
                                    case 2:
                                        {
                                            loop = false;
                                        }
                                        break;
                                }
                            }

                            // đẩy xuống đáy bộ nhớ để thực hiện lệnh tiếp theo
                            if (cmd_data.Type == CommandModbus_Service.CommandType.Repeat)// nếu là lệnh yêu cầu lặp lại
                            {
                                _commandQueue.Enqueue(cmd_data);
                            }

                            // nếu thực hiện hết 1 chu trình lệnh -> ngủ 1 khoảng thời gian
                            if (totalCommandExcute >= this.TotalCommandRepeat)
                            {
                                totalCommandExcute = 0;
                                Task task;
                                lock (_syncWaitHandleSleep)
                                {
                                    task = this.waitHandleSleep.WaitAsync(this._option.MilisecSleep, c);
                                }
                                await task.ConfigureAwait(false);
                            }
                        }
                    }
                        
                }
            }
            catch (Exception ex)
            {
                this.OnClosing(ex);
            }
        }
        private async Task ProcessInflightEvent(CancellationToken c)
        {
            while (!c.IsCancellationRequested)
            {
                try
                {
                    var queueItem = await _eventQueue.TryDequeueAsync(c).ConfigureAwait(false);
                    if (queueItem.IsSuccess)
                    {
                        var eventData = queueItem.Item;
                        if (eventData != null)
                        {
                            switch (eventData.Type)
                            {
                                case EventMsgHandle_Base.TYPE_RESPOND:
                                    {
                                        var event_response = ((EventMsgHandle_Response)eventData).MsgResponse;
                                        if (_recievedMessageEvent.HasHandlers)
                                            await _recievedMessageEvent.InvokeAsync(new MsgResponseModbus_EventArg(event_response, this));
                                        IModbusRequest rpcCommand = null;
                                        lock (_rpcCommandQueue)
                                        {
                                            if (_rpcCommandQueue.Count > 0)
                                            {
                                                rpcCommand = _rpcCommandQueue.Peek();
                                            }
                                        }
                                        if (rpcCommand != null)
                                        {
                                            if (MessageCompareModbus.Compare(rpcCommand, event_response.Request))
                                            {
                                                var modbusMessage = new ModbusMessage(event_response.Request, event_response.Response);
                                                ModbusCmdResponse_Success<ModbusMessage> success = new ModbusCmdResponse_Success<ModbusMessage>(modbusMessage);

                                                lock (_rpcCommandQueue)
                                                {
                                                    _rpcCommandQueue.Dequeue();
                                                }
                                                lock (_syncCommand)
                                                {
                                                    _taskCompleteSouceRpc?.TrySetResult(success);
                                                }
                                            }
                                            // so sánh lệnh gửi đi và lệnh phản hồi có khớp nhau không
                                        }
                                    }
                                    break;
                                case EventMsgHandle_Base.TYPE_NO_RESPOND:
                                    if (_noRespondMessageEvent.HasHandlers)
                                    {
                                        await _noRespondMessageEvent.InvokeAsync(new MsgNoResponseModbus_EventArg(
                                                            ((EventMsgHandle_NoResponse)eventData).Command_Request, this));
                                    }
                                    break;
                            }
                        }
                    }

                }
                catch (Exception)
                {

                }
               
            }
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
                
            }
        }

        private async Task CloseCoreAsync(Exception ex)
        {
            Interlocked.Exchange(ref _connectstatus, (int)EModbusConnectStatus.Closed);
            lock (_lockStop)
            {
                _backgroundCancelToken?.Cancel();
            }
            await WaitForTask(_taskReceieve).ConfigureAwait(false);
            await WaitForTask(_taskSendData).ConfigureAwait(false);
            await WaitForTask(_taskEvent).ConfigureAwait(false);
            await WaitForTask(_taskKeepAlive).ConfigureAwait(false);
            try
            {
                if (_clientChannel != null)
                     _clientChannel.DisconnectAsync();
            }
            catch (Exception)
            {

            }
            _stopQueue.Clear();
            _commandQueue.Clear();
            _eventQueue.Clear();
            if (_closedConnectionEvent.HasHandlers)
            {
                await _closedConnectionEvent.InvokeAsync(new MsgClosedConnectionEventArgs(new EventArgs(), this));
            }
            // stop all service in here
        }

        public async Task DisconnectAsync()
        {
            OnClosing(new Exception("stop by user"));
            try
            {
                if (_taskStop != null)
                {
                    await _taskStop.ConfigureAwait(false);
                }
            }
            catch (Exception)
            {

            }
        }

        public void Disconnect()
        {
            OnClosing(new Exception("stop by user"));
        }


        private async Task WaitForTask(Task task)
        {
            try
            {
                if (task != null && task.IsCanceled == false)
                    await task.ConfigureAwait(false);
            }
            catch (Exception)
            {

            }
        }
        

        private void OnClosing(Exception e)
        {
            lock (_lockStop)
            {
                if (!_backgroundCancelToken.Token.IsCancellationRequested)
                {
                    _stopQueue.Enqueue(e);
                }
            }
        }

        
    }
}
