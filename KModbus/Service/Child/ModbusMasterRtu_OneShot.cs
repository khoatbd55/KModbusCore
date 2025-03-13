using KModbus.Formatter;
using KModbus.IO;
using KModbus.Message;
using KModbus.Service.Data;
using KModbus.Service.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KModbus.Service
{
    public class ModbusMasterRtu_OneShot
    {
        AutoResetEvent waitHandleCommand;
        AutoResetEvent waitRespondCommand;
        Queue commandQueue;

        AutoResetEvent waitHandleEvent;
        Queue eventQueue;

        ModbusRtuTransport clientComport;


        private int totalCommand;
        private int commandExcute;
        public bool IsRunning { get
            {
                return _backgroundCancelToken!=null?!_backgroundCancelToken.IsCancellationRequested : false;
            }
        } 
        private bool isClosing;
        public bool IsConnected { get; set; }
        public int TimeOut { get; set; }
        IModbusResponse msgModbusRespond =new ReadCoilStatusResponse() ;
        List<Task> listTaskRun;
        CancellationTokenSource _backgroundCancelToken;
        private IModbusFormatter _modbusFormatter;

        public delegate void ModbusMasterRecieveMsgEventHandle(object sender, ModbusMessage msg, int commandExcute,int totalCommand);
        public delegate void ModbusMasterNoRespondEventHandle(object sender, EventMsgHandle_NoResponse e);
        public delegate void ModbusCloseConnectionEventHandle(object sender, EventArgs e);
        public delegate void ModbusMasterCompleteCommand(object sender, EventArgs e);
        public delegate void ModbusExceptionSerialEventHandle(object sender, Exception e);

        public event ModbusMasterRecieveMsgEventHandle RecievedMessage;
        public event ModbusMasterNoRespondEventHandle NoRespondMessage;
        public event ModbusCloseConnectionEventHandle ClosedConntion;
        public event ModbusMasterCompleteCommand CompleteCommand;
        public event ModbusExceptionSerialEventHandle OnExceptionSerial;

        private void OnRecievedMessage(ModbusMessage msg,int commandExcute)
        {
            if (this.RecievedMessage != null)
                this.RecievedMessage(this, msg, commandExcute,this.totalCommand);
        }
        private void OnNoRespondMessage(IModbusRequest msg)
        {
            if (this.NoRespondMessage != null)
                this.NoRespondMessage(this, new EventMsgHandle_NoResponse(msg));
        }
        private void OnCompleteCommand()
        {
            if(this.CompleteCommand!=null)
            {
                this.CompleteCommand(this, new EventArgs());
            }    
        }
        private void OnClosedConnection()
        {
            if (this.ClosedConntion != null)
                this.ClosedConntion(this, new EventArgs());
        }
        private void OnClosing()
        {
            if (this.isClosing == false)
            {
                this.isClosing = true;
                this.waitHandleEvent.Set();
            }

        }
        private async Task CloseAsync()
        {
            _backgroundCancelToken?.Cancel();
            this.IsConnected = false;

            try
            {
                await clientComport.DisconnectAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {

            }
            lock (commandQueue)
            {
                if (commandQueue != null)
                    commandQueue.Clear();
            }
            lock (eventQueue)
            {
                if (eventQueue != null)
                    eventQueue.Clear();
            }
            // giải phóng tất cả thread
            this.waitHandleCommand.Set();
            this.waitHandleEvent.Set();
            this.waitRespondCommand.Set();

        }
        public void Disconnect()
        {
            this.OnClosing();
        }
        public async Task DisconnectAsync()
        {
            this.OnClosing();
            try
            {
                await Task.WhenAll(listTaskRun).ConfigureAwait(false);
            }
            catch (Exception)
            {

            }

        }

        async Task WaitForTaskAsync(Task task, Task sender)
        {
            if (task == null)
            {
                return;
            }

            if (task == sender)
            {
                // Return here to avoid deadlocks, but first any eventual exception in the task
                // must be handled to avoid not getting an unhandled task exception
                if (!task.IsFaulted)
                {
                    return;
                }

                return;
            }

            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public ModbusMasterRtu_OneShot()
        {
            this.commandQueue = new Queue();
            this.eventQueue = new Queue();

            this.waitHandleCommand = new AutoResetEvent(false);
            this.waitHandleEvent = new AutoResetEvent(false);
            this.waitRespondCommand = new AutoResetEvent(false);
            this.listTaskRun = new List<Task>();
            _modbusFormatter = new ModbusRtuFormatter();
        }
        
        public async Task RunAsync(string nameComport,List<CommandModbus_Service> list_command,int timeOut,int baudrate)
        {
            await Comport_Init(nameComport, baudrate);
            // add list command 
            foreach (var item_cmd in list_command)
            {
                // đẩy toàn bộ lệnh vào queue để bắt đầu quy trình gửi lệnh liên tục lặp lại
                commandQueue.Enqueue(item_cmd);
            }
            this.totalCommand = commandQueue.Count;
            this.TimeOut = timeOut;
            _backgroundCancelToken = new CancellationTokenSource();
            var cancelToken = _backgroundCancelToken.Token;
            // start all thread 
            var task = Task.Run(()=> ProcessInflightCommand(cancelToken),cancelToken);
            listTaskRun.Add(task);
             task = Task.Run(()=> ProcessInflightEvent(cancelToken),cancelToken);
            listTaskRun.Add(task);
            
        }
        public async Task RunAsync(string nameComport, List<CommandModbus_Service> list_command)
        {
            await RunAsync(nameComport, list_command, 3000,9600);
        }

        public async Task RunAsync(string nameComport,List<CommandModbus_Service> list_cmd,int baudrate)
        {
            await RunAsync(nameComport, list_cmd, 3000, baudrate);
        }
        private void EnqueueCommand(CommandModbus_Service cmd_data)
        {
            lock (commandQueue)
            {
                commandQueue.Enqueue(cmd_data);
            }
            this.waitHandleCommand.Set();
        }
        private void EnqueueEvent(EventMsgHandle_Base eventData)
        {

            lock (eventQueue)
            {
                eventQueue.Enqueue(eventData);
            }
            this.waitHandleEvent.Set();
        }
        public void SendCommand_NoRepeat(IModbusRequest requestModbus)
        {
            CommandModbus_Service cmd = new CommandModbus_Service(requestModbus,CommandModbus_Service.CommandType.NoRepeat);
            EnqueueCommand(cmd);
        }
        private void ProcessInflightCommand(CancellationToken c)
        {
            try
            {
                while (!c.IsCancellationRequested)
                {
                    if (this.commandQueue.Count == 0 && !this.isClosing)
                    {
                        EventMsgHandle_Stop e_stop = new EventMsgHandle_Stop();
                        EnqueueEvent(e_stop);
                    }
                    if (!c.IsCancellationRequested)
                    {
                        CommandModbus_Service cmd_data = null;
                        lock (commandQueue)
                        {
                            if (commandQueue.Count > 0)
                            {
                                cmd_data = (CommandModbus_Service)commandQueue.Dequeue();
                            }
                        }
                        if (cmd_data != null)
                        {
                            bool loop = true;
                            int step = 0;
                            int retry = 0;
                            while (loop && this.IsRunning)
                            {
                                switch (step)
                                {
                                    case 0:// gửi lệnh
                                        clientComport.SendData(cmd_data.ModbusRequest);
                                        bool state = this.waitRespondCommand.WaitOne(this.TimeOut);
                                        if (state)// có phản hồi 
                                        {
                                            retry = 0;
                                            ModbusMessage msgModbus = new ModbusMessage(cmd_data.ModbusRequest, msgModbusRespond);
                                            Interlocked.Increment(ref commandExcute);
                                            EventMsgHandle_Response eventResponse = new EventMsgHandle_Response(msgModbus,commandExcute);
                                            EnqueueEvent(eventResponse);
                                            step = 1;
                                        }
                                        else
                                        {
                                            if (++retry >= 3)
                                            {
                                                step = 1;// không có phản hồi từ dưới thiết bị gửi lên
                                                EventMsgHandle_NoResponse eventNoResponse = new EventMsgHandle_NoResponse(cmd_data.ModbusRequest);
                                                EnqueueEvent(eventNoResponse);
                                            }
                                        }
                                        break;
                                    case 1:
                                        {
                                            loop = false;
                                        }
                                        break;
                                }
                            }
                        }
                        else// lệnh rống -> giải phóng luôn
                        {
                            throw new Exception("Lệnh gửi xuống thiết bị không được phép trống");
                        }
                    }
                }
            }
            catch (Exception)
            {
                this.OnClosing();
            }
        }
        private async Task ProcessInflightEvent(CancellationToken c)
        {
            while (!c.IsCancellationRequested)
            {
                if (eventQueue.Count == 0 && !this.isClosing)
                    this.waitHandleEvent.WaitOne();
                if (!c.IsCancellationRequested)
                {
                    EventMsgHandle_Base eventData = null;
                    lock (eventQueue)
                    {
                        if (eventQueue.Count > 0)
                            eventData = (EventMsgHandle_Base)eventQueue.Dequeue();
                    }
                    if (eventData != null)
                    {
                        switch (eventData.Type)
                        {
                            case EventMsgHandle_Base.TYPE_RESPOND:
                                {
                                    var e = (EventMsgHandle_Response)eventData;
                                    this.OnRecievedMessage(e.MsgResponse, e.PercentExcute);
                                }
                                break;
                            case EventMsgHandle_Base.TYPE_NO_RESPOND:
                                {
                                    this.OnClosing();
                                    this.OnNoRespondMessage(((EventMsgHandle_NoResponse)eventData).Command_Request);
                                }
                                break;
                            case EventMsgHandle_Base.TYPE_STOP:
                                {
                                    this.OnClosing();
                                    this.OnCompleteCommand();
                                }
                                break;
                            case EventMsgHandle_Base.TYPE_EXCEPTION_COMPORT:
                                {
                                    var msg = (EventMsgHandle_ExceptionSerial)eventData;
                                    if (this.OnExceptionSerial != null)
                                    {
                                        this.OnExceptionSerial(this, msg.Ex);
                                    }
                                    this.OnClosing();
                                }
                                break;


                        }
                    }
                    if (eventQueue.Count == 0 && this.isClosing)// all event queue raising and need closing 
                    {
                        await this.CloseAsync().ConfigureAwait(false);
                        this.OnClosedConnection();// raising event close connection
                    }
                }
            }
        }

        private async Task Comport_Init(string nameComport,int baudrate)
        {
            await clientComport.ConnectAsync();
            clientComport.MessageRecieved += ClientComport_MessageRecieved1;
            clientComport.Closed += ClientComport_Closed;
            clientComport.OnExceptionOccur += ClientComport_OnExceptionOccur;
        }

        private void ClientComport_OnExceptionOccur(object sender, Exception ex)
        {
            EventMsgHandle_ExceptionSerial msgEvent = new EventMsgHandle_ExceptionSerial(ex);
            EnqueueEvent(msgEvent);
        }

        private void ClientComport_MessageRecieved1(object sender, IModbusResponse e)
        {
            lock (msgModbusRespond)
            {
                msgModbusRespond = e;
            }
            this.waitRespondCommand.Set();
        }

        private void ClientComport_Closed(object sender, EventArgs e)
        {
            Disconnect();
        }
    }
}
