using KModbus.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Interfaces
{
    public delegate void MessageModbusEventHandle(object sender, IModbusResponse e);
    // Delegate for connection close
    public delegate void ConnectionCloseEventHandle(object sender, EventArgs e);
    //
    public delegate void ExceptionOccurEventHandle(object sender, Exception ex);
    public interface IMobusTransportAdapter
    {
        
        event ExceptionOccurEventHandle OnExceptionOccur;
        // event for Message Modbus Recieved
        event MessageModbusEventHandle MessageRecieved;
        // event for Connection closed
        event ConnectionCloseEventHandle Closed;
        Task ConnectAsync();
        Task SendDataAsync(IModbusRequest request);
        Task DisconnectAsync();
    }
}
