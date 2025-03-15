using KModbus.Config;
using KModbus.Data;
using KModbus.Data.Options;
using KModbus.Extention;
using KModbus.IO;
using KModbus.Message;
using KModbus.Service.Data.Mutile;
using KModbus.Service.Event;
using KModbus.Service.Event.Child;
using KModbus.Service.Event.Multile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KModbus.Service
{
    public class ModbusMasterRtuMultile_Runtime
    {
        public delegate void NoResponseEventHanlde(object sender, ModbusMultilNoResponse_EventArg e);
        public delegate void ResponseEventHandle(object sender, ModbusMultilResponse_EventArg e);
        public delegate void ClosedConnectionEventHandle(object sender, ModbusMultileClosedConnection_EventArg e);
        public delegate void ExceptionOccurEventHandle(object sender, ModbusMultileException_EventArg e);
        public delegate void AllSerialStopEventHandle(object sender, EventArgs e);

        public event NoResponseEventHanlde OnNoResponse;
        public event ResponseEventHandle OnResponse;
        public event ClosedConnectionEventHandle OnClosedConnection;
        public event ExceptionOccurEventHandle OnExceptionOccur;
        public event AllSerialStopEventHandle OnAllSerialStop;

        public bool IsRunning
        {
            get
            {
                if (_backgroundCancellationSource != null)
                {
                    return !_backgroundCancellationSource.IsCancellationRequested;
                }
                else
                    return false;
            }
        }

        Queue<EventHandleMultileService_Base> eventQueue;
        CancellationTokenSource _backgroundCancellationSource;
        SemaphoreSlim waitHandleEvent;
        bool isClosing = false;
        ModbusMasterRtuMultile_RuntimeCollection listModbusMaster;
        Task task_event;


        #region Rise Event 

        private void Event_NoResponse(string nameComport, MsgNoResponseModbus_EventArg msg)
        {
            if(this.OnNoResponse!=null)
            {
                this.OnNoResponse(this, new ModbusMultilNoResponse_EventArg(nameComport, msg));
            }    
        }


        private void Event_Response(string nameComport, MsgResponseModbus_EventArg msg)
        {
            if(this.OnResponse!=null)
            {
                this.OnResponse(this, new ModbusMultilResponse_EventArg(nameComport, msg));
            }    
        }

        private void Event_ClosedConnection(string nameComport)
        {
            if(this.OnClosedConnection!=null)
            {
                this.OnClosedConnection(this, new ModbusMultileClosedConnection_EventArg(nameComport));
            }    
        }

        private void Event_ExceptionOccur(string nameComport,Exception ex)
        {
            if(OnExceptionOccur!=null)
            {
                this.OnExceptionOccur(this, new ModbusMultileException_EventArg(nameComport, ex));
            }    
        }

        #endregion 

        public ModbusMasterRtuMultile_Runtime()
        {
            eventQueue = new Queue<EventHandleMultileService_Base>();
            listModbusMaster = new ModbusMasterRtuMultile_RuntimeCollection();
            waitHandleEvent = new SemaphoreSlim(0);
        }

        public async Task RunAsync(List<KModbusMasterOption> listOption,List< SerialPortOptions> serialOptions)
        {
            List<Task> listTask = new List<Task>();
            for(int i=0;i<listOption.Count;i++)
            {
                var option=listOption[i];
                ModbusMasterRtu_Runtime master_Runtime = new ModbusMasterRtu_Runtime(new ModbusRtuTransport(serialOptions[i]));
                master_Runtime.OnClosedConnectionAsync += Master_Runtime_OnClosedConnectionAsync;
                master_Runtime.OnExceptionAsync += Master_Runtime_OnExceptionAsync; ;
                master_Runtime.OnNoRespondMessageAsync += Master_Runtime_OnNoRespondMessageAsync; ;
                master_Runtime.OnRecievedMessageAsync += Master_Runtime_OnRecievedMessageAsync; ;
                listModbusMaster.Add(master_Runtime);
                var task = master_Runtime.RunAsync(option);
            }
            await Task.WhenAll(listTask).ConfigureAwait(false);// phải mở được cổng trước khi chạy
            _backgroundCancellationSource = new CancellationTokenSource();
            var c = _backgroundCancellationSource.Token;
            task_event = Task.Run(() => ProcessInflightEvent(c), c);
        }

        

        public async Task StopAsync()
        {
            this.isClosing = true;
            this.waitHandleEvent.Release();
            try
            {
                await task_event.ConfigureAwait(false);
            }
            catch (Exception)
            {

            }
        }

        private void OnClosing()
        {
            this.isClosing = true;
            this.waitHandleEvent.Release();
        }

        private async Task CloseAsync()
        {
            
            List<Task> listTask = new List<Task>();
            for(int i=0;i<listModbusMaster.Count;i++)
            {
                var task = listModbusMaster[i].DisconnectAsync();
                listTask.Add(task);
            }    
            try
            {
                await Task.WhenAll(listTask).ConfigureAwait(false);
            }
            catch (Exception)
            {
                
            }
            _backgroundCancellationSource.Cancel();
        }

        

        public void SendCommand_NoRepeat(string nameComport,IModbusRequest request)
        {
            var serialFind = listModbusMaster.FirstOrDefault(x => x.NameComport == nameComport);
            if (serialFind != null)
                serialFind.SendCommand_NoRepeat(request);
        }

        public void SendCommand_Repeat(string nameComport,IModbusRequest request)
        {
            var find = listModbusMaster.FirstOrDefault(x => x.NameComport == nameComport);
            if(find!=null)
            {
                find.SendCommnad_Repeat(request);
            }    
        }

        public async Task<ModbusCmdResponse_Base<ModbusMultilCmdResponse_Model>> SendCommandNoRepeatAsync(string nameComport,IModbusRequest request,CancellationToken c)
        {
            var find = listModbusMaster.FirstOrDefault(x => x.NameComport == nameComport);
            if(find==null)
            {
                var err = new ModbusCmdResponse_Error<ModbusMultilCmdResponse_Model>();
                err.ResultObj = new ModbusMultilCmdResponse_Model();
                err.Message = "Không tìm thấy cổng kết nối " + nameComport;
                return err;
            }  
            else
            {
                var response = await find.SendCommandNoRepeatAsync(request,c).ConfigureAwait(false);
                if(response.Type==EModbusCmdResponseType.Success)
                {
                    return new ModbusCmdResponse_Success<ModbusMultilCmdResponse_Model>(new ModbusMultilCmdResponse_Model()
                    {
                        NameComport=nameComport,
                        MsgModbus=response.ResultObj
                    });
                }
                else if(response.Type==EModbusCmdResponseType.Error)
                {
                    return new ModbusCmdResponse_Error<ModbusMultilCmdResponse_Model>(new ModbusMultilCmdResponse_Model()
                    {
                        NameComport=nameComport,
                        MsgModbus=response.ResultObj
                    });
                }
                else
                {
                    return new ModbusCmdResponse_NoResponse<ModbusMultilCmdResponse_Model>(new ModbusMultilCmdResponse_Model()
                    {
                        NameComport = nameComport,
                        MsgModbus = response.ResultObj
                    });
                }    
            }    
            
        }


        private async Task ProcessInflightEvent(CancellationToken c)
        {
            while (!c.IsCancellationRequested)
            {
                if (eventQueue.Count == 0 && !this.isClosing)
                    await this.waitHandleEvent.WaitAsync(c).ConfigureAwait(false);
                if (!c.IsCancellationRequested)
                {
                    EventHandleMultileService_Base msgEvent = null;
                    lock (eventQueue)
                    {
                        if (this.eventQueue.Count > 0)
                        {
                            msgEvent = this.eventQueue.Dequeue();
                        }
                    }
                    if (msgEvent != null)
                    {
                        switch (msgEvent.Type)
                        {
                            case ETypeEventMultileService.Response:
                                {
                                    EventHandleMultileService_Response response = (EventHandleMultileService_Response)msgEvent;
                                    Event_Response(response.NameComport, response.MsgResponse);
                                }
                                break;
                            case ETypeEventMultileService.NoResponse:
                                {
                                    EventHandleMultileService_NoResponse noResponse = (EventHandleMultileService_NoResponse)msgEvent;
                                    Event_NoResponse(noResponse.NameComport, noResponse.MsgNoResponse);
                                }
                                break;
                            case ETypeEventMultileService.ExceptionOccur:
                                {
                                    EventHandleMultileService_Exception exception = (EventHandleMultileService_Exception)msgEvent;
                                    Event_ExceptionOccur(exception.NameComport, exception.ExOccur);
                                }
                                break;
                            case ETypeEventMultileService.ClosedConnection:
                                {
                                    EventHandleMultileService_ClosedConnection closedConnection = (EventHandleMultileService_ClosedConnection)msgEvent;
                                    Event_ClosedConnection(closedConnection.NameComport);
                                }
                                break;

                        }
                    }
                    if ((eventQueue.Count == 0) && this.isClosing)
                    {
                        await this.CloseAsync().ConfigureAwait(false);
                        // báo hiệu tất cả các cổng đã đóng và dừng hoạt động
                        if(this.OnAllSerialStop!=null)
                        {
                            this.OnAllSerialStop(this, new EventArgs());
                        }    
                    }
                }
            }
        }

        private void EnqueueEvent(EventHandleMultileService_Base baseEvent)
        {
            lock(eventQueue)
            {
                eventQueue.Enqueue(baseEvent);
                waitHandleEvent.Release();
            }    

        }

        private Task Master_Runtime_OnRecievedMessageAsync(MsgResponseModbus_EventArg arg)
        {
            var modbusMaster = (ModbusMasterRtu_Runtime)arg.Sender;
            EventHandleMultileService_Response responseEvent = new EventHandleMultileService_Response(modbusMaster.NameComport, arg);
            EnqueueEvent(responseEvent);
            return Task.CompletedTask;
        }

        private Task Master_Runtime_OnNoRespondMessageAsync(MsgNoResponseModbus_EventArg arg)
        {
            var modbusMaster = (ModbusMasterRtu_Runtime)arg.Sender;
            EventHandleMultileService_NoResponse noResponse = new EventHandleMultileService_NoResponse(modbusMaster.NameComport, arg);
            EnqueueEvent(noResponse);
            return Task.CompletedTask;
        }

        
        private Task Master_Runtime_OnExceptionAsync(Event.Child.MsgExceptionEventArgs arg)
        {
            var modbusMaster = (ModbusMasterRtu_Runtime)arg.Sender;
            EventHandleMultileService_Exception exception = new EventHandleMultileService_Exception(modbusMaster.NameComport, arg.Ex);
            EnqueueEvent(exception);
            return Task.CompletedTask;
        }

        private Task Master_Runtime_OnClosedConnectionAsync(MsgClosedConnectionEventArgs arg)
        {
            lock (this.listModbusMaster)
            {
                var service = (ModbusMasterRtu_Runtime)arg.Sender;
                this.listModbusMaster.Remove(service);
            }
            EventHandleMultileService_ClosedConnection msg = new EventHandleMultileService_ClosedConnection(
                                        ((ModbusMasterRtu_Runtime)arg.Sender).NameComport);
            EnqueueEvent(msg);
            // tất cả child service đã hoàn thành 
            if (this.listModbusMaster.Count == 0)
            {
                this.OnClosing();
            }
            return Task.CompletedTask;
        }


        private void Master_Runtime_OnClosedConntion(object sender, EventArgs e)
        {
            
        }



    }
}
