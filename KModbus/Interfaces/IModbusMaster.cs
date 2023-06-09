using KModbus.Data;
using KModbus.Message;
using KModbus.Service.Event;
using KModbus.Service.Event.Child;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KModbus.Interfaces
{

    public interface IModbusMaster
    {
        event Func<MsgResponseModbus_EventArg, Task> OnRecievedMessageAsync;
        event Func<MsgNoResponseModbus_EventArg,Task> OnNoRespondMessageAsync;
        event Func<MsgClosedConnectionEventArgs, Task> OnClosedConnectionAsync;
        event Func<MsgExceptionEventArgs, Task> OnExceptionAsync;

        bool IsRunning { get; }
        Task DisconnectAsync();
        void Disconnect();
        void SendCommand_NoRepeat(IModbusRequest requestModbus);
        void SendCommnad_Repeat(IModbusRequest requestModbus);
        Task<ModbusCmdResponse_Base<ModbusMessage>> SendCommandNoRepeatAsync(IModbusRequest request, CancellationToken c);
    }
}
