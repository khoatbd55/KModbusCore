KModbus
=======

KModbus is a C# implementation of the Modbus protocol.
Provides connectivity to Modbus slave compatible devices and applications.
Supports serial serial RTU, TCP, asynchronous support,high performance, multi serial ,auto reconnect 
Project includes demo code


Install
=======

To install KModbus, run the following command in the Package Manager Console

    PM> Install-Package KModbus

Performance
Optimized performance, consumption on arm32 arm64 core cpu compared to microsoft serialport driver library
With normal cpu diff arm
var adapter = new ModbusRtuTransport(new SerialPortOptions()
{
    Baudrate = 9600,
    DataBit = 8,
    Parity = System.IO.Ports.Parity.None,
    PortName = "COM11",
    StopBit = System.IO.Ports.StopBits.One,
});

With cpu arm
var adapter = new ModbusRtuLinuxTransport(new SerialPortOptions()
{
    Baudrate = 9600,
    DataBit = 8,
    Parity = System.IO.Ports.Parity.None,
    PortName = "COM11",
    StopBit = System.IO.Ports.StopBits.One,
});
ModbusMasterRtu_Runtime modbusMaster = new ModbusMasterRtu_Runtime(adapter);

License
=======
KModbus is licensed under the [MIT license](LICENSE.txt).
