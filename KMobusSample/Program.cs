// See https://aka.ms/new-console-template for more information
using KModbus;
using KModbus.Data.Options;
using KModbus.IO;
using KModbus.Message;
using KModbus.Service;
using KModbus.Service.Model;
using KUtilities.ConvertExtentions;


//var adapter = new ModbusRtuLinuxTransport(new SerialPortOptions()
//{
//    Baudrate = 9600,
//    DataBit = 8,
//    Parity = System.IO.Ports.Parity.None,
//    PortName = "COM11",
//    StopBit = System.IO.Ports.StopBits.One,
//});
//var adapter = new ModbusMqttTransport(new MqttModbusOptions()
//{
//    DeviceId=101,
//    HeaderTopicRequest="mqtt/dac/request/",
//    HeaderTopicResponse="mqtt/dac/response/",
//    Host="localhost",
//    Password= "dac54321",
//    Port=19030,
//    UserName= "mqttdac"
//});
var tcpOption = new ModbusClientTcpChannelOptions();
tcpOption.Server = "192.168.144.201";
tcpOption.Port = 502;
tcpOption.Timeout = TimeSpan.FromSeconds(10);
var adapter = new ModbusTcpClientTransport(new ModbusClientTcpOptions()
{
    PacketProtocal = EModbusPacketProtocal.TcpIp,
    TcpOption = tcpOption,
    TimeOutConnect=10,
    TransactionId=1
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
    //modbusMaster.RunAutoConnectAsync(new KModbus.Config.KModbusMasterOption()
    //{
    //    DelayResponse = 10,
    //    IsAutoReconnect = true,
    //    ListCmd = listCmd,
    //    MsSleep = 0,
    //    WaitResponse = 1500,
    //    Retry = 1
    //});

    await modbusMaster.RunAsync(new KModbus.Config.KModbusMasterOption()
    {
        DelayResponse = 10,
        IsAutoReconnect = true,
        ListCmd = listCmd,
        MsSleep = 0,
        WaitResponse = 1500,
        Retry = 1
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
    try
    {
        var res1 = await modbusMaster.SendCommandNoRepeatAsync(new ReadHoldingRegisterRequest(241, 0, 4), new CancellationTokenSource().Token);
        if (res1.Type == KModbus.Data.EModbusCmdResponseType.Success)
        {
            var request = (ReadHoldingRegisterRequest)res1.ResultObj.Request;
            var response = (ReadHoldingRegisterResponse)res1.ResultObj.Response;
            var f_reg = response.Register;
            var b_reg = ConvertReg.ConvertArrayUint16ToByte(response.Register);
            int idx = 0;
            var RH = ConvertVariable.BytesToFloat(b_reg, ref idx);
            var TEMP = ConvertVariable.BytesToFloat(b_reg, ref idx);
            Console.WriteLine("RH:{0},TEMP:{1}", RH, TEMP);
            index++;
            //Console.WriteLine("{2}-adr input request {0} ,register response: [{1}]", request.AddressRegister, string.Join(", ", f_reg),index);
            if (index % 1000 == 0)
            {
                Console.Clear();
            }
        }
        var res2 = await modbusMaster.SendCommandNoRepeatAsync(new ReadHoldingRegisterRequest(240, 42, 2), new CancellationTokenSource().Token);
        if (res2.Type == KModbus.Data.EModbusCmdResponseType.Success)
        {
            var request = (ReadHoldingRegisterRequest)res2.ResultObj.Request;
            var response = (ReadHoldingRegisterResponse)res2.ResultObj.Response;
            var f_reg = response.Register;
            var b_reg = ConvertReg.ConvertArrayUint16ToByte(response.Register);
            int idx = 0;
            var Pressire = ConvertVariable.BytesToFloat(b_reg, ref idx);
            Console.WriteLine("PRessure:{0}", Pressire);
            index++;
            //Console.WriteLine("{2}-adr input request {0} ,register response: [{1}]", request.AddressRegister, string.Join(", ", f_reg), index);
            if (index % 1000 == 0)
            {
                Console.Clear();
            }
        }
        
    }
    catch (Exception ex)
    {
        Console.WriteLine("modbus send exception:{0}",ex.Message); 
    }
    await Task.Delay(500);
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