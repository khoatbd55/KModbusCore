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
    public class ModbusMasterRtu_Runtime : IModbusMaster
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

        KPriorityQueueAsync<CommandModbus_Service> _commandQueue = new KPriorityQueueAsync<CommandModbus_Service>();
        KAsyncQueue<EventMsgHandle_Base> _eventQueue = new KAsyncQueue<EventMsgHandle_Base>();

        SemaphoreSlim _waitHandleSleep;
        SemaphoreSlim _waitCheckCountQueue;

        IMobusTransportAdapter _clientAdapter;

        private int _msSleep;
        private int _delayResponse;
        private int _totalCommandRepeat;
        private int _isComportOpened = 0;

        KModbusMasterOption _option;

        CancellationTokenSource _backgroundCancelTokenSource = new CancellationTokenSource();
        CancellationTokenSource _sendCmdCancelTokenSource = new CancellationTokenSource();
        int _connectstatus = (int)(EModbusConnectStatus.Closed);
        List<TaskCompleteSouceRpcModel> _listTaskRpc = new List<TaskCompleteSouceRpcModel>();
        KAsyncTaskCompletionSource<IModbusResponse> _taskCompleteSouceMessage = new KAsyncTaskCompletionSource<IModbusResponse>();
        Task _taskStop;
        KAsyncQueue<Exception> _stopQueue = new KAsyncQueue<Exception>();

        Task _taskCommand;
        Task _taskEvent;
        Task _taskAutoReconnect;
        object _lockStop = new object();
        object _syncAutoReconnect = new object();
        object _syncListTaskRpc = new object();
        readonly object _syncCommand = new object();
        readonly object _syncMessage = new object();
        readonly object _syncWaitHandleSleep = new object();

        public string NameComport 
        {
            get
            {
                if (_option != null)
                    return _option.NameId;
                else
                    return "";
            }
        }// xem lại cái này

        public bool IsConnected { get; set; }

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
                if (!_backgroundCancelTokenSource.Token.IsCancellationRequested)
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
            this._waitHandleSleep = new SemaphoreSlim(0);
            this._waitCheckCountQueue = new SemaphoreSlim(0);
            _taskCompleteSouceMessage = new KAsyncTaskCompletionSource<IModbusResponse>();
        }

        public async Task RunAsync(KModbusMasterOption option)
        {
            this._option = option;
            this._msSleep = option.MsSleep;
            this._delayResponse = option.DelayResponse;
            await Comport_InitAsync();
            Interlocked.Exchange(ref _isComportOpened, 1);// trạng thái báo hiệu comport đã mở
            _backgroundCancelTokenSource = new CancellationTokenSource();
            _commandQueue = new KPriorityQueueAsync<CommandModbus_Service>();
            _eventQueue = new KAsyncQueue<EventMsgHandle_Base>();
            if (option.ListCmd != null)
            {
                foreach (var item in option.ListCmd)
                {
                    EnqueueCommand(item, ECmdPriority.Default);
                }
            }
            CancellationToken c = _backgroundCancelTokenSource.Token;
            _stopQueue = new KAsyncQueue<Exception>();
            _sendCmdCancelTokenSource = new CancellationTokenSource();
            var ctLink = CancellationTokenSource.CreateLinkedTokenSource(c, _sendCmdCancelTokenSource.Token);
            _taskCommand = Task.Run(() => ProcessInflightCommand(ctLink.Token), ctLink.Token);
            _taskEvent = Task.Run(() => ProcessInflightEvent(c), c);
            _taskStop = Task.Run(() => ProcessStopAllTask(c), c);
            _taskAutoReconnect = Task.Run(() => ProcessAutoReconnect(c), c);
            Interlocked.Exchange(ref _connectstatus, (int)EModbusConnectStatus.Opened);
            WriteLog(EModbusLogType.Infomation, "modbus running");
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
                            if (_sendCmdCancelTokenSource.IsCancellationRequested == false)
                            {
                                _sendCmdCancelTokenSource?.Cancel();
                                isCancel = true;
                            }
                        }
                        if (isCancel)
                        {
                            // chở cho task send đóng hẳn
                            await WaitForTask(_taskCommand).ConfigureAwait(false);
                        }
                        WriteLog(EModbusLogType.Infomation, "reconnect modbus ");
                        // cố gắng mở lại kết nối
                        await Comport_InitAsync();
                        Interlocked.Exchange(ref _isComportOpened, 1);// trạng thái báo hiệu comport đã mở
                        _sendCmdCancelTokenSource = new CancellationTokenSource();
                        var ctLink = CancellationTokenSource.CreateLinkedTokenSource(c, _sendCmdCancelTokenSource.Token);
                        _taskCommand = Task.Run(() => ProcessInflightCommand(ctLink.Token), ctLink.Token);
                        WriteLog(EModbusLogType.Infomation, "reconnect success, modbus running...");
                    }
                }
                catch (Exception ex)
                {
                    EventMsgHandle_ExceptionSerial msgEvent = new EventMsgHandle_ExceptionSerial(ex);
                    EnqueueEvent(msgEvent);
                }
                await Task.Delay(500, c).ConfigureAwait(false);
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
                //_logger.LogError("{time} {0} cache service - fail in stop task {1}", DateTime.Now, this._name, e.Message);
            }
        }

        public async Task CloseCoreAsync(Exception ex)
        {
            Interlocked.Exchange(ref _connectstatus, (int)EModbusConnectStatus.Closed);
            lock (_lockStop)
            {
                _backgroundCancelTokenSource?.Cancel();
            }
            lock (_syncAutoReconnect)
            {
                _sendCmdCancelTokenSource?.Cancel();
            }
            await _clientAdapter.DisconnectAsync().ConfigureAwait(false);
            await WaitForTask(_taskCommand).ConfigureAwait(false);
            await WaitForTask(_taskAutoReconnect).ConfigureAwait(false);
            await WaitForTask(_taskEvent).ConfigureAwait(false);
            _commandQueue.Clear();
            _eventQueue.Clear();
            lock (_syncListTaskRpc)
            {
                _listTaskRpc.Clear();
            }

            this._waitCheckCountQueue.Release();
            if (_closedConnectionEvent.HasHandlers)
            {
                await _closedConnectionEvent.InvokeAsync(new MsgClosedConnectionEventArgs(new EventArgs(), this)).ConfigureAwait(false);
            }
        }

        private void EnqueueCommand(CommandModbus_Service cmd_data, ECmdPriority pirority)
        {
            // chỉ cho phép gửi lệnh khi cổng serial đã mở 
            if (_isComportOpened == 1)
            {
                lock (_commandQueue)
                {
                    if (cmd_data.Type == CommandModbus_Service.CommandType.Repeat)
                        this._totalCommandRepeat++;
                    _commandQueue.Enqueue(cmd_data, (int)pirority);
                }
                lock (_syncWaitHandleSleep)
                {
                    this._waitHandleSleep.Release();
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
            if (this.IsRunning)
            {
                Guid id = Guid.NewGuid();
                var _taskCompleteSouceRpc = new KAsyncTaskCompletionSource<ModbusCmdResponse_Base<ModbusMessage>>();// khởi tạo dữ liệu phản hồi
                TaskCompleteSouceRpcModel rpc = new TaskCompleteSouceRpcModel(_taskCompleteSouceRpc, id);
                // trước khi gửi lệnh sẽ thêm vào list task rpc
                lock (_syncListTaskRpc)
                {
                    _listTaskRpc.Add(rpc);
                }
                ModbusCmdResponse_Base<ModbusMessage> response = null;
                // gửi lệnh đi
                EnqueueCommand(new CommandModbus_Service(request, CommandModbus_Service.CommandType.NoRepeat, id), priority);
                using (var ctLink = CancellationTokenSource.CreateLinkedTokenSource(c, _backgroundCancelTokenSource.Token))
                using (var ctTimeOut = new CancellationTokenSource(timeOut))
                {
                    var ct = CancellationTokenSource.CreateLinkedTokenSource(ctLink.Token, ctTimeOut.Token);
                    var register = ct.Token.Register(() =>
                    {
                        lock (_syncListTaskRpc)
                        {
                            _taskCompleteSouceRpc.TrySetCanceled();
                        }
                    }, useSynchronizationContext: true);
                    Task task;
                    lock (_syncListTaskRpc)
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
                        ct.Dispose();
                        response = _taskCompleteSouceRpc.Task.Result;
                    }
                    else
                    {
                        register.Dispose();
                        ct.Dispose();
                        ModbusCmdResponse_NoResponse<ModbusMessage> noResponse = new ModbusCmdResponse_NoResponse<ModbusMessage>();
                        noResponse.ResultObj = new ModbusMessage(request, null);
                        response = noResponse;
                        return noResponse;
                    }
                }
                // xóa bỏ task rpc khỏi 
                lock (_syncListTaskRpc)
                {
                    _listTaskRpc.Remove(rpc);
                }
                return response;
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
                    var queueItem = await _commandQueue.TryDequeueAsync(c).ConfigureAwait(false);
                    if (queueItem.IsSuccess && !c.IsCancellationRequested)
                    {
                        CommandModbus_Service cmd_data = queueItem.Item;
                        if (cmd_data != null)
                        {
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
                                    case 0:// gửi lệnh
                                        {
                                            _taskCompleteSouceMessage = new KAsyncTaskCompletionSource<IModbusResponse>();
                                            await _clientAdapter.SendDataAsync(cmd_data.ModbusRequest);
                                            using (var ctTimeOut = new CancellationTokenSource(_option.WaitResponse))
                                            using (var ctLink = CancellationTokenSource.CreateLinkedTokenSource(c, ctTimeOut.Token))
                                            {
                                                var register = ctLink.Token.Register(() =>
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
                                                    register.Dispose();// 
                                                    retry = 0;
                                                    this.IsConnected = true;
                                                    ModbusMessage msgModbus = new ModbusMessage(cmd_data.ModbusRequest, _taskCompleteSouceMessage.Task.Result);
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
                                                    // trễ 1 khoảng thời gian sau khi nhận được phản hổi từ modbus slaver
                                                    await Task.Delay(this._delayResponse, c).ConfigureAwait(false);
                                                }
                                                else
                                                {
                                                    register.Dispose();// 
                                                    if (++retry >= 2)
                                                    {
                                                        step = 2;// không có phản hồi từ dưới thiết bị gửi lên
                                                        this.IsConnected = false;
                                                        EnqueueEvent(new EventMsgHandle_NoResponse(cmd_data.ModbusRequest));
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

                            // đẩy xuống đáy bộ nhớ để thực hiện lệnh tiếp theo
                            if (cmd_data.Type == CommandModbus_Service.CommandType.Repeat)// nếu là lệnh yêu cầu lặp lại
                            {
                                _commandQueue.Enqueue(cmd_data, (int)ECmdPriority.Default);
                            }

                            // nếu thực hiện hết 1 chu trình lệnh -> ngủ 1 khoảng thời gian
                            if (totalCommandExcute >= this._totalCommandRepeat)
                            {
                                totalCommandExcute = 0;

                                if (this._msSleep > 0)
                                {
                                    Task task;
                                    lock (_syncWaitHandleSleep)
                                    {
                                        task = this._waitHandleSleep.WaitAsync(this._msSleep, c);
                                    }
                                    await task.ConfigureAwait(false);
                                }
                            }
                        }
                    }

                }
            }
            catch (Exception)
            {

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
                                    WriteLog(EModbusLogType.Error, "serial port closed", msg.Ex);
                                }
                                break;
                        }
                    }
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

        private async Task Comport_InitAsync()
        {
            await _clientAdapter.ConnectAsync();
            _clientAdapter.MessageRecieved += ClientComport_MessageRecieved1;
            _clientAdapter.OnExceptionOccur += ClientComport_OnExceptionOccur;
            _clientAdapter.Closed += ClientComport_Closed; ;
        }

        private void ClientComport_Closed(object sender, EventArgs e)
        {
            Interlocked.Exchange(ref _isComportOpened, 0);
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
                _taskCompleteSouceMessage.TrySetResult(e);
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
