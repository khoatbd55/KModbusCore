KModbus
=======

NModbus is a C# implementation of the Modbus protocol.
Provides connectivity to Modbus slave compatible devices and applications.
Supports serial serial RTU, TCP, asynchronous support,high performent, multi serial 



KModbus differs from NModbus4 in following:

- Modbus slave devices are now added to a network which is represented by `IModbusSlaveInstance`.
- Heavier use of interfaces.
- Custom function code handlers can be added to slave devices.


Goals
=======
- Improve Modbus Slave support (e.g. support multiple slave devices on the same physical transport).

Install
=======

To install NModbus, run the following command in the Package Manager Console

    PM> Install-Package KModbus


License
=======
KModbus is licensed under the [MIT license](LICENSE.txt).
