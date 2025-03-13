using KModbus.Formatter;
using KModbus.Message;
using KUtilities.TaskExtentions;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KModbus.IO
{
    public class ModbusMqttTransport
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

        private IModbusFormatter _modbusFormatter;
        Task _taskRecv;
        Task _taskStop;
        Task _taskSendData;
        KAsyncQueue<Exception> _stopQueue = new KAsyncQueue<Exception>();
        object _lockStop = new object();
        KAsyncQueue<MsgComportRecv> _recvQueue = new KAsyncQueue<MsgComportRecv>();
        KAsyncQueue<byte[]> _sendQueue = new KAsyncQueue<byte[]>();

        private CancellationTokenSource _backgroundCancellationSource = new CancellationTokenSource();

        IMqttClient _mqttClient;

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
