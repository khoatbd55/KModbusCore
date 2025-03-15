using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Data.Options
{
    public class MqttModbusOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 1883;
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public int DeviceId { get; set; } = 1;
        public string HeaderTopicRequest { get; set; } = "modbus/request/";
        public string HeaderTopicResponse { get; set; } = "modbus/response/";
    }
}
