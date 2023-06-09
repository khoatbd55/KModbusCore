// See https://aka.ms/new-console-template for more information
using KModbus;
using KModbus.Message;
using KModbus.Service;
using KModbus.Service.Model;
using System.Net.WebSockets;

ModbusMasterRtu_Runtime modbusMaster = new ModbusMasterRtu_Runtime();
modbusMaster.OnRecievedMessageAsync += ModbusMaster_OnRecievedMessageAsync;
modbusMaster.OnNoRespondMessageAsync += ModbusMaster_OnNoRespondMessageAsync;
modbusMaster.OnExceptionAsync += ModbusMaster_OnExceptionAsync;
modbusMaster.OnClosedConnectionAsync += ModbusMaster_OnClosedConnectionAsync;

List<CommandModbus_Service> listCmd = new List<CommandModbus_Service>();
var cmd = new ReadHoldingRegisterRequest(100, 0, 10);
listCmd.Add(new CommandModbus_Service(cmd, CommandModbus_Service.CommandType.Repeat));
Console.WriteLine("try openning comport");
await modbusMaster.RunAsync(new KModbus.Config.KModbusMasterOption("COM7", 10, listCmd, 10, 9600));
Console.WriteLine("modbus master running");

while(true)
{
    var res= await modbusMaster.SendCommandNoRepeatAsync(new ReadInputRegisterRequest(100, 0, 10), new CancellationTokenSource().Token);
    if (res.Type == KModbus.Data.EModbusCmdResponseType.Success)
    {
        var request = (ReadInputRegisterRequest)res.ResultObj.Request;
        var response = (ReadInputRegisterResponse)res.ResultObj.Response;
        Console.WriteLine("adr input request {0} ,register response: [{1}]", request.AddressRegister, string.Join(", ", response.Register));
    }
    await Task.Delay(100);
}    
Task ModbusMaster_OnClosedConnectionAsync(KModbus.Service.Event.Child.MsgClosedConnectionEventArgs arg)
{
    Console.WriteLine("modbus master closed connection");
    return Task.CompletedTask;
}

Task ModbusMaster_OnExceptionAsync(KModbus.Service.Event.Child.MsgExceptionEventArgs arg)
{
    Console.WriteLine("modbus master exception .detail {0}",arg.Ex.Message); 
    return Task.CompletedTask;
}

Task ModbusMaster_OnNoRespondMessageAsync(KModbus.Service.Event.MsgNoResponseModbus_EventArg arg)
{
    Console.WriteLine("modbus master no response .slave id:{0} func:{1}",arg.Request.SlaverAddress,arg.Request.FuntionCode);
    return Task.CompletedTask;
}

Task ModbusMaster_OnRecievedMessageAsync(KModbus.Service.Event.MsgResponseModbus_EventArg arg)
{
    Console.WriteLine("modbus master recv message . slave id:{0} func:{1}", arg.Message.Response.SlaverAddress, arg.Message.Response.FuntionCode);
    if (arg.Message.Request.FuntionCode == ModbusFunctionCodes.ReadHoldingRegisters)
    {
        var request=(ReadHoldingRegisterRequest)arg.Message.Request;
        var response=(ReadHoldingRegisterResponse)arg.Message.Response;
        Console.WriteLine("adr holding request {0} ,register response: [{1}]", request.AddressRegister, string.Join(", ", response.Register));
    }
    else if(arg.Message.Request.FuntionCode==ModbusFunctionCodes.ReadInputRegisters)
    {

    }
    return Task.CompletedTask;    
}