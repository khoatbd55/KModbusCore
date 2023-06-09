using KModbus.Config;
using KModbus.Data.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Events
{
    public class ModbusClientCertificateValidationEventArgs
    {
        public X509Certificate Certificate { get; set; }
        public X509Chain Chain { get; set; }
        public SslPolicyErrors SslPolicyErrors { get; set; }
        public ModbusClientTcpOptions Options { get; set; }
    }
}
