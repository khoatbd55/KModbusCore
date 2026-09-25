using KModbus.Config;
using KModbus.Data;
using KModbus.Data.Model;
using KModbus.Data.Services;
using KModbus.Events;
using KModbus.Extention;
using KModbus.Formatter;
using KModbus.Interfaces;
using KModbus.IO;
using KModbus.Message;
using KModbus.Message.Handles;
using KModbus.Service.Data;
using KModbus.Service.Data.Child;
using KModbus.Service.Event;
using KModbus.Service.Event.Child;
using KModbus.Service.Model;
using KUtilities.TaskExtentions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KModbus.Service
{
    public class ModbusMasterRtu_Runtime : IModbusMaster, IDisposable
    {
        public delegate void ModbusMasterLogEventHandle(object sender, ModbusLogEventArgs e);
        public event ModbusMasterLogEventHandle OnLog;

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

        private KPriorityQueueAsync<CommandModbus_Service> _commandQueue = new KPriorityQueueAsync<CommandModbus_Service>();
        private KAsyncQueue<EventMsgHandle_Base> _eventQueue = new KAsyncQueue<EventMsgHandle_Base>();

        private readonly SemaphoreSlim _wakeUpSleepSignal = new SemaphoreSlim(0, 1);
        private readonly IMobusTransportAdapter _clientAdapter;

        private int _msSleep;
        private int _delayResponse;
        private int _totalCommandRepeat;
        private int _isComportOpened = 0;

        private KModbusMasterOption _option;

        private CancellationTokenSource _backgroundCancelTokenSource = new CancellationTokenSource();
        private CancellationTokenSource _sendCmdCancelTokenSource = new CancellationTokenSource();
        private CancellationTokenSource _linkedCmdCancelTokenSource;
        private int _connectstatus = (int)EModbusConnectStatus.Closed;
        private readonly List<TaskCompleteSouceRpcModel> _listTaskRpc = new List<TaskCompleteSouceRpcModel>();
        private KAsyncTaskCompletionSource<IModbusResponse> _taskCompleteSourceMessage = new KAsyncTaskCompletionSource<IModbusResponse>();
        private IModbusRequest _currentInflightRequest;
        private Task _taskStop;
        private KAsyncQueue<Exception> _stopQueue = new KAsyncQueue<Exception>();

        private Task _taskCommand;
        private Task _taskEvent;
        private Task _taskAutoReconnect;
        private readonly object _lockStop = new object();
        private readonly object _syncAutoReconnect = new object();
        private readonly object _syncListTaskRpc = new object();
        private readonly object _syncMessage = new object();
        private readonly object _syncWaitHandleSleep = new object();

        public string NameComport 
        {
            get
            {
                return _option?.NameId ?? "";
            }
        }

        private volatile bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => _isConnected = value;
        }

        public int TotalQueueCommand
        {
            get { return this._commandQueue.Count; }
        }

        public bool IsRunning
        {
            get
            {
                return ((EModbusConnectStatus)_connectstatus) == EModbusConnectStatus.Opened;
            }
        }

        public enum ECmdPriority
        {
            Priority = 0, // càng thấp càng ưu tiên cao
            Default,
        }

        private void OnClosing(Exception e)
        {
            lock (_lockStop)
            {
                if (_backgroundCancelTokenSource != null && !_backgroundCancelTokenSource.Token.IsCancellationRequested)
                {
                    _stopQueue.Enqueue(e);
                }
            }
        }

        public void Disconnect()
        {
            this.OnClosing(new Exception("disconnect by require"));
        }

        public async Task DisconnectAsync()
        {
            this.OnClosing(new Exception("disconnect by require"));
            await WaitForTask(_taskStop).ConfigureAwait(false);
        }

        public ModbusMasterRtu_Runtime(IMobusTransportAdapter adapter)
        {
            this._clientAdapter = adapter;
            _listTaskRpc = new List<TaskCompleteSouceRpcModel>();
            _taskCompleteSourceMessage = new KAsyncTaskCompletionSource<IModbusResponse>();
        }

        public async Task RunAsync(KModbusMasterOption option)
        {
            if (this.IsRunning)
            {
                await DisconnectAsync().ConfigureAwait(false);
            }

            this._option = option;
            this._msSleep = option.MsSleep;
            this._delayResponse = option.DelayResponse;

            _totalCommandRepeat = 0;
            _commandQueue = new KPriorityQueueAsync<CommandModbus_Service>();
            _eventQueue = new KAsyncQueue<EventMsgHandle_Base>();

            if (option.ListCmd != null)
            {
                lock (_commandQueue)
                {
                    foreach (var item in option.ListCmd)
                    {
                        if (item.Type == CommandModbus_Service.CommandType.Repeat)
                            this._totalCommandRepeat++;
                        _commandQueue.Enqueue(item, (int)ECmdPriority.Default);
                    }
                }
            }

            await Comport_InitAsync().ConfigureAwait(false);
            
            Interlocked.Exchange(ref _isComportOpened, 1);
            Interlocked.Exchange(ref _connectstatus, (int)EModbusConnectStatus.Opened);

            lock (_lockStop)
            {
                _backgroundCancelTokenSource?.Dispose();
                _backgroundCancelTokenSource = new CancellationTokenSource();
            }
            CancellationToken c = _backgroundCancelTokenSource.Token;

            _stopQueue = new KAsyncQueue<Exception>();

            lock (_syncAutoReconnect)
            {
                _linkedCmdCancelTokenSource?.Dispose();
                _sendCmdCancelTokenSource?.Dispose();
                _sendCmdCancelTokenSource = new CancellationTokenSource();
                _linkedCmdCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(c, _sendCmdCancelTokenSource.Token);
            }

            _taskCommand = Task.Run(() => ProcessInflightCommand(_linkedCmdCancelTokenSource.Token), _linkedCmdCancelTokenSource.Token);
            _taskEvent = Task.Run(() => ProcessInflightEvent(c), c);
            _taskStop = Task.Run(() => ProcessStopAllTask(c), c);
            if (option.IsAutoReconnect)
            {
                _taskAutoReconnect = Task.Run(() => ProcessAutoReconnect(c), c);
            }
            WriteLog(EModbusLogType.Infomation, "modbus running");
        }

        public void RunAutoConnectAsync(KModbusMasterOption option)
        {
            this._option = option;
            this._msSleep = option.MsSleep;
            this._delayResponse = option.DelayResponse;

            _totalCommandRepeat = 0;
            _commandQueue = new KPriorityQueueAsync<CommandModbus_Service>();
            _eventQueue = new KAsyncQueue<EventMsgHandle_Base>();

            if (option.ListCmd != null)
            {
                lock (_commandQueue)
                {
                    foreach (var item in option.ListCmd)
                    {
                        if (item.Type == CommandModbus_Service.CommandType.Repeat)
                            this._totalCommandRepeat++;
                        _commandQueue.Enqueue(item, (int)ECmdPriority.Default);
                    }
                }
            }

            lock (_lockStop)
            {
                _backgroundCancelTokenSource?.Dispose();
                _backgroundCancelTokenSource = new CancellationTokenSource();
            }
            CancellationToken c = _backgroundCancelTokenSource.Token;

            _stopQueue = new KAsyncQueue<Exception>();

            lock (_syncAutoReconnect)
            {
                _linkedCmdCancelTokenSource?.Dispose();
                _sendCmdCancelTokenSource?.Dispose();
                _sendCmdCancelTokenSource = new CancellationTokenSource();
                _linkedCmdCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(c, _sendCmdCancelTokenSource.Token);
            }

            _taskCommand = Task.Run(() => ProcessInflightCommand(_linkedCmdCancelTokenSource.Token), _linkedCmdCancelTokenSource.Token);
            _taskEvent = Task.Run(() => ProcessInflightEvent(c), c);
            _taskStop = Task.Run(() => ProcessStopAllTask(c), c);
            _taskAutoReconnect = Task.Run(() => ProcessAutoReconnect(c), c);
            WriteLog(EModbusLogType.Infomation, "modbus auto connect starting...");
        }

        private async Task ProcessAutoReconnect(CancellationToken c)
        {
            while (!c.IsCancellationRequested)
            {
                try
                {
                    if (_isComportOpened == 0)
                    {
                        bool isCancel = false;
                        // nếu task gửi dữ liệu chưa dừng thì dừng
                        lock (_syncAutoReconnect)
                        {
                            if (_sendCmdCancelTokenSource != null && !_sendCmdCancelTokenSource.IsCancellationRequested)
                            {
                                _sendCmdCancelTokenSource.Cancel();
                                isCancel = true;
                            }
                        }
                        if (isCancel)
                        {
                            // chờ cho task send đóng hẳn
                            await WaitForTask(_taskCommand).ConfigureAwait(false);
                        }
                        WriteLog(EModbusLogType.Infomation, "reconnect modbus ");
                        
                        // cố gắng mở lại kết nối
                        await Comport_InitAsync().ConfigureAwait(false);
                        Interlocked.Exchange(ref _isComportOpened, 1);
                        Interlocked.Exchange(ref _connectstatus, (int)EModbusConnectStatus.Opened);
                        
                        lock (_syncAutoReconnect)
                        {
                            _linkedCmdCancelTokenSource?.Dispose();
                            _sendCmdCancelTokenSource?.Dispose();
                            _sendCmdCancelTokenSource = new CancellationTokenSource();
                            _linkedCmdCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(c, _sendCmdCancelTokenSource.Token);
                        }
                        _taskCommand = Task.Run(() => ProcessInflightCommand(_linkedCmdCancelTokenSource.Token), _linkedCmdCancelTokenSource.Token);
                        WriteLog(EModbusLogType.Infomation, "reconnect success, modbus running...");
                    }
                }
                catch (OperationCanceledException) when (c.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    EventMsgHandle_ExceptionSerial msgEvent = new EventMsgHandle_ExceptionSerial(ex);
                    EnqueueEvent(msgEvent);
                }

                try
                {
                    await Task.Delay(1000, c).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
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

        public async Task CloseCoreAsync(Exception ex)
        {
            Interlocked.Exchange(ref _connectstatus, (int)EModbusConnectStatus.Closed);
            Interlocked.Exchange(ref _isComportOpened, 0);
            this.IsConnected = false;

            lock (_lockStop)
            {
                _backgroundCancelTokenSource?.Cancel();
            }
            lock (_syncAutoReconnect)
            {
                _sendCmdCancelTokenSource?.Cancel();
                _linkedCmdCancelTokenSource?.Dispose();
                _linkedCmdCancelTokenSource = null;
            }

            UnsubscribeAdapterEvents();

            try
            {
                await _clientAdapter.DisconnectAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
            }

            await WaitForTask(_taskCommand).ConfigureAwait(false);
            await WaitForTask(_taskAutoReconnect).ConfigureAwait(false);
            await WaitForTask(_taskEvent).ConfigureAwait(false);

            _commandQueue.Clear();
            _eventQueue.Clear();

            lock (_syncListTaskRpc)
            {
                foreach (var rpc in _listTaskRpc)
                {
                    var noResponse = new ModbusCmdResponse_NoResponse<ModbusMessage>
                    {
                        Message = "Kết nối đã đóng"
                    };
                    rpc.TaskCompleteSource.TrySetResult(noResponse);
                }
                _listTaskRpc.Clear();
            }

            WakeUpSleepIfWaiting();

            if (_closedConnectionEvent.HasHandlers)
            {
                await _closedConnectionEvent.InvokeAsync(new MsgClosedConnectionEventArgs(new EventArgs(), this)).ConfigureAwait(false);
            }
        }

        private void EnqueueCommand(CommandModbus_Service cmd_data, ECmdPriority priority)
        {
            if (this.IsRunning || _isComportOpened == 1)
            {
                lock (_commandQueue)
                {
                    if (cmd_data.Type == CommandModbus_Service.CommandType.Repeat)
                        this._totalCommandRepeat++;
                    _commandQueue.Enqueue(cmd_data, (int)priority);
                }

                // Nếu là lệnh ưu tiên hoặc lệnh NoRepeat, đánh thức chu kỳ ngủ (nếu đang sleep)
                if (priority == ECmdPriority.Priority || cmd_data.Type == CommandModbus_Service.CommandType.NoRepeat)
                {
                    WakeUpSleepIfWaiting();
                }
            }
        }

        private void WakeUpSleepIfWaiting()
        {
            lock (_syncWaitHandleSleep)
            {
                if (_wakeUpSleepSignal.CurrentCount == 0)
                {
                    try
                    {
                        _wakeUpSleepSignal.Release();
                    }
                    catch (SemaphoreFullException)
                    {
                    }
                }
            }
        }

        private void EnqueueEvent(EventMsgHandle_Base eventData)
        {
            lock (_eventQueue)
            {
                _eventQueue.Enqueue(eventData);
            }
        }

        public void SendCommand_NoRepeat(IModbusRequest requestModbus)
        {
            if (this.IsRunning)
            {
                CommandModbus_Service cmd = new CommandModbus_Service(requestModbus, CommandModbus_Service.CommandType.NoRepeat, Guid.NewGuid());
                EnqueueCommand(cmd, ECmdPriority.Default);
            }
        }

        public void SendCommnad_Repeat(IModbusRequest requestModbus)
        {
            if (this.IsRunning)
            {
                CommandModbus_Service cmd = new CommandModbus_Service(requestModbus, CommandModbus_Service.CommandType.Repeat, Guid.NewGuid());
                EnqueueCommand(cmd, ECmdPriority.Default);
            }
        }

        public void SendCommand_Repeat(IModbusRequest requestModbus)
        {
            SendCommnad_Repeat(requestModbus);
        }

        public async Task<ModbusCmdResponse_Base<ModbusMessage>> SendCommandNoRepeatAsync(IModbusRequest request, CancellationToken c)
        {
            return await SendCommandNoRepeatAsync(request, 5000, ECmdPriority.Default, c).ConfigureAwait(false);
        }

        public async Task<ModbusCmdResponse_Base<ModbusMessage>> SendCommandNoRepeatAsync(IModbusRequest request, int timeOut, CancellationToken c)
        {
            return await SendCommandNoRepeatAsync(request, timeOut, ECmdPriority.Default, c).ConfigureAwait(false);
        }

        public async Task<ModbusCmdResponse_Base<ModbusMessage>> SendCommandNoRepeatAsync(IModbusRequest request, ECmdPriority priority, CancellationToken c)
        {
            return await SendCommandNoRepeatAsync(request, 5000, priority, c).ConfigureAwait(false);
        }

        public async Task<ModbusCmdResponse_Base<ModbusMessage>> SendCommandNoRepeatAsync(IModbusRequest request,
                                                                    int timeOut, ECmdPriority priority, CancellationToken c)
        {
            if (!this.IsRunning)
            {
                throw new Exception("Cổng mất kết nối - không thể gửi lệnh ");
            }

            Guid id = Guid.NewGuid();
            var taskCompleteSouceRpc = new KAsyncTaskCompletionSource<ModbusCmdResponse_Base<ModbusMessage>>();
            TaskCompleteSouceRpcModel rpc = new TaskCompleteSouceRpcModel(taskCompleteSouceRpc, id);

            lock (_syncListTaskRpc)
            {
                _listTaskRpc.Add(rpc);
            }

            try
            {
                EnqueueCommand(new CommandModbus_Service(request, CommandModbus_Service.CommandType.NoRepeat, id), priority);

                using (var ctLink = CancellationTokenSource.CreateLinkedTokenSource(c, _backgroundCancelTokenSource.Token))
                using (var ctTimeOut = new CancellationTokenSource(timeOut))
                using (var ct = CancellationTokenSource.CreateLinkedTokenSource(ctLink.Token, ctTimeOut.Token))
                using (ct.Token.Register(() =>
                {
                    taskCompleteSouceRpc.TrySetCanceled();
                }, useSynchronizationContext: false))
                {
                    try
                    {
                        return await taskCompleteSouceRpc.Task.ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        ModbusCmdResponse_NoResponse<ModbusMessage> noResponse = new ModbusCmdResponse_NoResponse<ModbusMessage>
                        {
                            ResultObj = new ModbusMessage(request, null)
                        };
                        return noResponse;
                    }
                }
            }
            finally
            {
                lock (_syncListTaskRpc)
                {
                    _listTaskRpc.Remove(rpc);
                }
            }
        }

        private async Task ProcessInflightCommand(CancellationToken c)
        {
            int totalCommandExcute = 0;
            while (!c.IsCancellationRequested)
            {
                try
                {
                    var queueItem = await _commandQueue.TryDequeueAsync(c).ConfigureAwait(false);
                    if (!queueItem.IsSuccess || c.IsCancellationRequested)
                    {
                        continue;
                    }

                    CommandModbus_Service cmd_data = queueItem.Item;
                    if (cmd_data == null)
                    {
                        continue;
                    }

                    Guid commandId = cmd_data.Id;
                    bool loop = true;
                    int step = 0;
                    int retry = 0;

                    if (cmd_data.Type == CommandModbus_Service.CommandType.Repeat)
                        totalCommandExcute++;

                    while (loop && !c.IsCancellationRequested)
                    {
                        switch (step)
                        {
                            case 0: // gửi lệnh
                                {
                                    lock (_syncMessage)
                                    {
                                        _currentInflightRequest = cmd_data.ModbusRequest;
                                        _taskCompleteSourceMessage = new KAsyncTaskCompletionSource<IModbusResponse>();
                                    }

                                    await _clientAdapter.SendDataAsync(cmd_data.ModbusRequest).ConfigureAwait(false);

                                    int waitTimeout = _option?.WaitResponse ?? 1000;
                                    using (var ctTimeOut = new CancellationTokenSource(waitTimeout))
                                    using (var ctLink = CancellationTokenSource.CreateLinkedTokenSource(c, ctTimeOut.Token))
                                    using (ctLink.Token.Register(() =>
                                    {
                                        lock (_syncMessage)
                                        {
                                            _taskCompleteSourceMessage?.TrySetCanceled();
                                        }
                                    }, useSynchronizationContext: false))
                                    {
                                        Task<IModbusResponse> task;
                                        lock (_syncMessage)
                                        {
                                            task = _taskCompleteSourceMessage.Task;
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

                                        if (!isCancellation) // có phản hồi
                                        {
                                            retry = 0;
                                            this.IsConnected = true;
                                            ModbusMessage msgModbus = new ModbusMessage(cmd_data.ModbusRequest, task.Result);
                                            EnqueueEvent(new EventMsgHandle_Response(msgModbus));
                                            loop = false;

                                            // xử lí rpc
                                            lock (_syncListTaskRpc)
                                            {
                                                var find = _listTaskRpc.Find(x => x.Id == commandId);
                                                if (find != null)
                                                {
                                                    var modbusMessage = new ModbusMessage(cmd_data.ModbusRequest, msgModbus.Response);
                                                    ModbusCmdResponse_Success<ModbusMessage> success = new ModbusCmdResponse_Success<ModbusMessage>(modbusMessage);
                                                    find.TaskCompleteSource.TrySetResult(success);
                                                }
                                            }

                                            // trễ 1 khoảng thời gian sau khi nhận được phản hồi từ modbus slave
                                            if (this._delayResponse > 0)
                                            {
                                                await Task.Delay(this._delayResponse, c).ConfigureAwait(false);
                                            }
                                        }
                                        else // không có phản hồi cho lượt gửi này
                                        {
                                            int maxRetry = _option?.Retry ?? 1;
                                            if (++retry >= maxRetry)
                                            {
                                                step = 2; // hết số lần retry
                                                this.IsConnected = false;
                                                EnqueueEvent(new EventMsgHandle_NoResponse(cmd_data.ModbusRequest));

                                                // Báo ngay lập tức cho RPC caller biết là NoResponse
                                                lock (_syncListTaskRpc)
                                                {
                                                    var find = _listTaskRpc.Find(x => x.Id == commandId);
                                                    if (find != null)
                                                    {
                                                        var noResponse = new ModbusCmdResponse_NoResponse<ModbusMessage>
                                                        {
                                                            ResultObj = new ModbusMessage(cmd_data.ModbusRequest, null)
                                                        };
                                                        find.TaskCompleteSource.TrySetResult(noResponse);
                                                    }
                                                }
                                            }
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

                    lock (_syncMessage)
                    {
                        _currentInflightRequest = null;
                    }

                    // đẩy xuống đáy bộ nhớ để thực hiện lệnh tiếp theo (nếu là lệnh lặp)
                    if (cmd_data.Type == CommandModbus_Service.CommandType.Repeat)
                    {
                        _commandQueue.Enqueue(cmd_data, (int)ECmdPriority.Default);
                    }

                    // nếu thực hiện hết 1 chu trình lệnh lặp -> ngủ 1 khoảng thời gian
                    if (this._totalCommandRepeat > 0 && totalCommandExcute >= this._totalCommandRepeat)
                    {
                        totalCommandExcute = 0;

                        if (this._msSleep > 0)
                        {
                            // Drain các tín hiệu đánh thức còn sót lại trước khi vào chu kỳ ngủ mới
                            while (_wakeUpSleepSignal.Wait(0)) { }
                            await _wakeUpSleepSignal.WaitAsync(this._msSleep, c).ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException) when (c.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    lock (_syncMessage)
                    {
                        _currentInflightRequest = null;
                    }
                    WriteLog(EModbusLogType.Error, "Lỗi trong tiến trình gửi lệnh: " + ex.Message, ex);
                    EventMsgHandle_ExceptionSerial msgEvent = new EventMsgHandle_ExceptionSerial(ex);
                    EnqueueEvent(msgEvent);
                    Interlocked.Exchange(ref _isComportOpened, 0);
                    this.IsConnected = false;
                    try
                    {
                        await Task.Delay(500, c).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task ProcessInflightEvent(CancellationToken c)
        {
            while (!c.IsCancellationRequested)
            {
                try
                {
                    var msgQueue = await _eventQueue.TryDequeueAsync(c).ConfigureAwait(false);
                    if (msgQueue.IsSuccess)
                    {
                        var eventData = msgQueue.Item;
                        switch (eventData.Type)
                        {
                            case EventMsgHandle_Base.TYPE_LOG:
                                {
                                    var event_response = (EventMsgHandle_Log)eventData;
                                    if (this.OnLog != null)
                                    {
                                        this.OnLog.Invoke(this, new ModbusLogEventArgs(event_response.LogType, event_response.Message, event_response.Ex));
                                    }
                                }
                                break;
                            case EventMsgHandle_Base.TYPE_RESPOND:
                                {
                                    var event_response = ((EventMsgHandle_Response)eventData).MsgResponse;
                                    if (_recievedMessageEvent.HasHandlers)
                                    {
                                        await _recievedMessageEvent.InvokeAsync(
                                            new MsgResponseModbus_EventArg(event_response, this)).ConfigureAwait(false);
                                    }
                                }
                                break;
                            case EventMsgHandle_Base.TYPE_NO_RESPOND:
                                {
                                    if (_noRespondMessageEvent.HasHandlers)
                                    {
                                        await _noRespondMessageEvent.InvokeAsync(
                                            new MsgNoResponseModbus_EventArg(((EventMsgHandle_NoResponse)eventData).Command_Request, this)).ConfigureAwait(false);
                                    }
                                    WriteLog(EModbusLogType.Warning, "device no response");
                                }
                                break;
                            case EventMsgHandle_Base.TYPE_EXCEPTION_COMPORT:
                                {
                                    var msg = (EventMsgHandle_ExceptionSerial)eventData;
                                    if (_exceptionEvent.HasHandlers)
                                    {
                                        await _exceptionEvent.InvokeAsync(new MsgExceptionEventArgs(msg.Ex, this)).ConfigureAwait(false);
                                    }
                                }
                                break;
                        }
                    }
                }
                catch (OperationCanceledException) when (c.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception)
                {
                }
            }
        }

        private void WriteLog(EModbusLogType type, string message, Exception ex)
        {
            EnqueueEvent(new EventMsgHandle_Log(type, message, ex));
        }

        private void WriteLog(EModbusLogType type, string message)
        {
            EnqueueEvent(new EventMsgHandle_Log(type, message));
        }

        private void SubscribeAdapterEvents()
        {
            UnsubscribeAdapterEvents();
            if (_clientAdapter != null)
            {
                _clientAdapter.MessageRecieved += ClientComport_MessageRecieved1;
                _clientAdapter.OnExceptionOccur += ClientComport_OnExceptionOccur;
                _clientAdapter.Closed += ClientComport_Closed;
            }
        }

        private void UnsubscribeAdapterEvents()
        {
            if (_clientAdapter != null)
            {
                _clientAdapter.MessageRecieved -= ClientComport_MessageRecieved1;
                _clientAdapter.OnExceptionOccur -= ClientComport_OnExceptionOccur;
                _clientAdapter.Closed -= ClientComport_Closed;
            }
        }

        private async Task Comport_InitAsync()
        {
            SubscribeAdapterEvents();
            await _clientAdapter.ConnectAsync().ConfigureAwait(false);
        }

        private void ClientComport_Closed(object sender, EventArgs e)
        {
            Interlocked.Exchange(ref _isComportOpened, 0);
            this.IsConnected = false;
        }

        private void ClientComport_OnExceptionOccur(object sender, Exception ex)
        {
            EventMsgHandle_ExceptionSerial msgEvent = new EventMsgHandle_ExceptionSerial(ex);
            EnqueueEvent(msgEvent);
        }

        private void ClientComport_MessageRecieved1(object sender, IModbusResponse e)
        {
            lock (_syncMessage)
            {
                if (_currentInflightRequest != null && e != null)
                {
                    bool isAddressMatch = (e.SlaverAddress == _currentInflightRequest.SlaverAddress);
                    bool isFunctionMatch = (e.FuntionCode == _currentInflightRequest.FuntionCode ||
                                            e.FuntionCode == (_currentInflightRequest.FuntionCode | 0x80));
                    if (!isAddressMatch || !isFunctionMatch)
                    {
                        // Bỏ qua gói tin không khớp với request hiện tại
                        return;
                    }
                }
                _taskCompleteSourceMessage?.TrySetResult(e);
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

        public void Dispose()
        {
            try
            {
                Disconnect();
            }
            catch
            {
            }

            UnsubscribeAdapterEvents();

            lock (_syncAutoReconnect)
            {
                _linkedCmdCancelTokenSource?.Dispose();
                _linkedCmdCancelTokenSource = null;
                _sendCmdCancelTokenSource?.Dispose();
                _sendCmdCancelTokenSource = null;
            }

            lock (_lockStop)
            {
                _backgroundCancelTokenSource?.Dispose();
                _backgroundCancelTokenSource = null;
            }

            _wakeUpSleepSignal?.Dispose();
        }
    }
}
