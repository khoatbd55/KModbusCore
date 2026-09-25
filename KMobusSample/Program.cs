// See https://aka.ms/new-console-template for more information
using KModbus;
using KModbus.Data.Options;
using KModbus.IO;
using KModbus.Message;
using KModbus.Service;
using KModbus.Service.Model;
using KUtilities.ConvertExtentions;


var adapter = new ModbusRtuTransport(new SerialPortOptions()
{
    Baudrate = 19200,
    DataBit = 8,
    Parity = System.IO.Ports.Parity.None,
    PortName = "COM1",
    StopBit = System.IO.Ports.StopBits.One,
    DtrEnable = true,
    RtsEnable = false
});
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
//var tcpOption = new ModbusClientTcpChannelOptions();
//tcpOption.Server = "192.168.144.201";
//tcpOption.Port = 502;
//tcpOption.Timeout = TimeSpan.FromSeconds(10);
//var adapter = new ModbusTcpClientTransport(new ModbusClientTcpOptions()
//{
//    PacketProtocal = EModbusPacketProtocal.TcpIp,
//    TcpOption = tcpOption,
//    TimeOutConnect=10,
//    TransactionId=1
//});
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
        var resDataReady = await modbusMaster.SendCommandNoRepeatAsync(new ReadInputRegisterRequest(15, 0, 16), new CancellationToken());
        if(resDataReady.Type==KModbus.Data.EModbusCmdResponseType.Success)
        {
            var response = (ReadInputRegisterResponse)resDataReady.ResultObj.Response;            
            var f_reg = ConvertReg.ConvertArrayUin16ToFloat(response.Register);
            index++;
            Console.WriteLine("Ref:{0},INA0:{1},INA1:{2},INA2:{3}", f_reg[0], f_reg[1], f_reg[2], f_reg[3]);
            Console.WriteLine("DAC1:{0},DAC2:{1},DAC2:{2},DAC3:{3}\r\n", f_reg[4], f_reg[5], f_reg[6], f_reg[7]);
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
    await Task.Delay(100);
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