using KModbus.Data.Options;
using KModbus.Formatter;
using KModbus.Interfaces;
using KModbus.Message;
using KUtilities.TaskExtentions;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KModbus.IO
{
    public class ModbusMqttTransport: IMobusTransportAdapter
    {

        public event ExceptionOccurEventHandle OnExceptionOccur;
        // event for Message Modbus Recieved
        public event MessageModbusEventHandle MessageRecieved;
        // event for Connection closed
        public event ConnectionCloseEventHandle Closed;

        private IModbusFormatter _modbusFormatter = new ModbusRtuFormatter();
        Task _taskStop;
        Task _taskSendData;
        KAsyncQueue<Exception> _stopQueue = new KAsyncQueue<Exception>();
        object _lockStop = new object();
        KAsyncQueue<MsgComportRecv> _recvQueue = new KAsyncQueue<MsgComportRecv>();
        KAsyncQueue<byte[]> _sendQueue = new KAsyncQueue<byte[]>();

        private CancellationTokenSource _backgroundCancellationSource = new CancellationTokenSource();
        MqttModbusOptions _config = new MqttModbusOptions();
        IMqttClient _mqttClient;

        public ModbusMqttTransport(MqttModbusOptions config)
        {
            _config = config;
        }

        public async Task ConnectAsync()
        {
            _backgroundCancellationSource?.Cancel();
            _backgroundCancellationSource = new CancellationTokenSource();
            var c = _backgroundCancellationSource.Token;
            try
            {
                var options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer(_config.Host, _config.Port)
                .WithCredentials(_config.UserName, _config.Password)
                .WithCleanSession()
                .WithKeepAlivePeriod(new TimeSpan(0, 0, 30))
                .Build();
                _mqttClient = new MqttFactory().CreateMqttClient();
                await _mqttClient.ConnectAsync(options, c);
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                                    .WithTopic(_config.HeaderTopicResponse+_config.DeviceId)
                                    .WithAtMostOnceQoS()
                                    .Build());
                _mqttClient.ApplicationMessageReceivedAsync += _mqttClient_ApplicationMessageReceivedAsync; ;
                _mqttClient.DisconnectedAsync += _mqttClient_DisconnectedAsync; ;
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message, ex.InnerException);
            }
            _recvQueue = new KAsyncQueue<MsgComportRecv>();
            _sendQueue = new KAsyncQueue<byte[]>();
            
            _stopQueue = new KAsyncQueue<Exception>();            
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
                        var msg = new MqttApplicationMessageBuilder()
                        .WithTopic(_config.HeaderTopicRequest+_config.DeviceId)
                        .WithPayload(itemQueue.Item)
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                        .Build();
                        await _mqttClient.PublishAsync(msg, c);
                    }
                }
                catch (Exception ex)
                {
                    this.OnConnectionClosing(ex);
                }
            }

        }

        public Task SendDataAsync(IModbusRequest request)
        {
            SendData(request);
            return Task.CompletedTask;
        }

        public void SendData(IModbusRequest request)
        {
            var buf = _modbusFormatter.Create(request);
            _sendQueue.Enqueue(buf);
        }

        private Task _mqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            Exception ex = new Exception("disconnected mqtt");
            if (arg.Exception != null)
            {
                ex=arg.Exception;
            }
            this.EventExceptionOccur(ex);
            this.OnConnectionClosing(ex);
            return Task.CompletedTask;
        }

        private Task _mqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            byte[] frame = arg.ApplicationMessage.PayloadSegment.Array;
            var msgRespond = _modbusFormatter.DecodeMessage(frame);// dữ liệu phản hồi
            this.OnMessageRecieve(msgRespond);
            return Task.CompletedTask;
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
            await _mqttClient.DisconnectAsync();
            _recvQueue.Clear();
            _ = Task.Run(() =>
            {
                // run in task avoid deadlook
                if (Closed != null)
                    Closed.Invoke(this, new EventArgs());
            });

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
