# KModbus

**KModbus** is a C# implementation of the Modbus protocol.  
It provides connectivity to Modbus slave compatible devices and applications.

✨ Features:
- Supports **Serial RTU** and **TCP**
- Asynchronous communication
- High performance
- Multi-serial port support
- Auto reconnect
- Demo code included

---

## 📦 Install

To install **KModbus**, run the following command in the **Package Manager Console**:

```powershell
PM> Install-Package KModbus
```

---

## ⚡ Performance

**KModbus** is optimized for low CPU usage on `arm32` / `arm64` cores, especially when compared to Microsoft's built-in `SerialPort` driver.

---

### 💻 With normal CPU (e.g. Windows PC)

```csharp
var adapter = new ModbusRtuTransport(new SerialPortOptions()
{
    Baudrate = 9600,
    DataBit = 8,
    Parity = System.IO.Ports.Parity.None,
    PortName = "COM11",
    StopBit = System.IO.Ports.StopBits.One,
});
```

---

### 🍓 With ARM-based CPU (e.g. Raspberry Pi)

```csharp
var adapter = new ModbusRtuLinuxTransport(new SerialPortOptions()
{
    Baudrate = 9600,
    DataBit = 8,
    Parity = System.IO.Ports.Parity.None,
    PortName = "COM11",
    StopBit = System.IO.Ports.StopBits.One,
});

ModbusMasterRtu_Runtime modbusMaster = new ModbusMasterRtu_Runtime(adapter);
```

---

## 📝 License

KModbus is licensed under the [MIT license](LICENSE.txt).
