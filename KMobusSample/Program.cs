// See https://aka.ms/new-console-template for more information
using KModbus;
using KModbus.Data.Options;
using KModbus.IO;
using KModbus.Message;
using KModbus.Service;
using KModbus.Service.Model;


//var adapter = new ModbusRtuTransport(new SerialPortOptions()
//{
//    Baudrate=9600,
//    DataBit=8,
//    Parity=System.IO.Ports.Parity.None,
//    PortName="COM4",
//    StopBit=System.IO.Ports.StopBits.One,
//});
var adapter = new ModbusMqttTransport(new MqttModbusOptions()
{
    DeviceId=101,
    HeaderTopicRequest="mqtt/dac/request/",
    HeaderTopicResponse="mqtt/dac/response/",
    Host="localhost",
    Password= "dac54321",
    Port=19030,
    UserName= "mqttdac"
});
ModbusMasterRtu_Runtime modbusMaster = new ModbusMasterRtu_Runtime(adapter);
modbusMaster.OnRecievedMessageAsync += ModbusMaster_OnRecievedMessageAsync;
modbusMaster.OnNoRespondMessageAsync += ModbusMaster_OnNoRespondMessageAsync;
modbusMaster.OnExceptionAsync += ModbusMaster_OnExceptionAsync;
modbusMaster.OnClosedConnectionAsync += ModbusMaster_OnClosedConnectionAsync;

List<CommandModbus_Service> listCmd = new List<CommandModbus_Service>();
//var cmd = new ReadHoldingRegisterRequest(100, 257, 13);
//listCmd.Add(new CommandModbus_Service(cmd, CommandModbus_Service.CommandType.Repeat));
Console.WriteLine("try openning comport");
try
{
    await modbusMaster.RunAsync(new KModbus.Config.KModbusMasterOption()
    {
        DelayResponse=10,
        IsAutoReconnect=true,
        ListCmd=listCmd,
        MsSleep=0,
        WaitResponse=1500,
        Retry=1
    });
    Console.WriteLine("modbus master running,auto reconnect");
}
catch (Exception ex)
{
    Console.WriteLine("error open serial port.{0}",ex.Message);
    Console.ReadKey();
    return;
}


int index = 0;
while(true)
{
    var res= await modbusMaster.SendCommandNoRepeatAsync(new ReadInputRegisterRequest(4, 0, 30), new CancellationTokenSource().Token);
    if (res.Type == KModbus.Data.EModbusCmdResponseType.Success)
    {
        var request = (ReadInputRegisterRequest)res.ResultObj.Request;
        var response = (ReadInputRegisterResponse)res.ResultObj.Response;
        var f_reg = response.Register;
        double ppmv = ((double)f_reg[7]);
        double pw = ((double)f_reg[8]) / 10;
        double pws = ((double)f_reg[9]) / 10;
        
        double hpa = 1000000 * pw / ppmv + pw;
        double rh = pw * 100 / pws;
        index++;
        Console.WriteLine("{2}-adr input request {0} ,register response: [{1}]", request.AddressRegister, string.Join(", ", f_reg),index);
        if(index%1000==0)
        {
            Console.Clear();
        }    
    }
    await Task.Delay(1);
}    
Task ModbusMaster_OnClosedConnectionAsync(KModbus.Service.Event.Child.MsgClosedConnectionEventArgs arg)
{
    Console.WriteLine("modbus master closed connection");
    return Task.CompletedTask;
}

Task ModbusMaster_OnExceptionAsync(KModbus.Service.Event.Child.MsgExceptionEventArgs arg)
{
    Console.WriteLine("modbus master exception .detail {0}",arg.Ex?.Message); 
    return Task.CompletedTask;
}

Task ModbusMaster_OnNoRespondMessageAsync(KModbus.Service.Event.MsgNoResponseModbus_EventArg arg)
{
    Console.WriteLine("modbus master no response .slave id:{0} func:{1}",arg.Request.SlaverAddress,arg.Request.FuntionCode);
    return Task.CompletedTask;
}

Task ModbusMaster_OnRecievedMessageAsync(KModbus.Service.Event.MsgResponseModbus_EventArg arg)
{
    //Console.WriteLine("modbus master recv message . slave id:{0} func:{1}", arg.Message.Response.SlaverAddress, arg.Message.Response.FuntionCode);
    //if (arg.Message.Request.FuntionCode == ModbusFunctionCodes.ReadHoldingRegisters)
    //{
    //    var request=(ReadHoldingRegisterRequest)arg.Message.Request;
    //    var response=(ReadHoldingRegisterResponse)arg.Message.Response;
    //    Console.WriteLine("adr holding request {0} ,register response: [{1}]", request.AddressRegister, string.Join(", ", response.Register));
    //}
    //else if(arg.Message.Request.FuntionCode==ModbusFunctionCodes.ReadInputRegisters)
    //{

    //}
    return Task.CompletedTask;    
}