using KModbus.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Data.Options
{
    public class ModbusClientTlsOptions
    {
        public Func<ModbusClientCertificateValidationEventArgs, bool> CertificateValidationHandler { get; set; }

        public bool UseTls { get; set; }

        public bool IgnoreCertificateRevocationErrors { get; set; }

        public bool IgnoreCertificateChainErrors { get; set; }

        public bool AllowUntrustedCertificates { get; set; }

#if WINDOWS_UWP
        public List<byte[]> Certificates { get; set; }
#else
        public List<X509Certificate> Certificates { get; set; }
#endif

#if NETCOREAPP3_1 || NET5_0_OR_GREATER
        public List<System.Net.Security.SslApplicationProtocol> ApplicationProtocols { get; set; }
#endif

#if NET48 || NETCOREAPP3_1 || NET5 || NET6 || NET7 || NET8 || NET9 
        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12 | SslProtocols.Tls13;
#else
        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12 | (SslProtocols)0x00003000 /*Tls13*/;
#endif
    }
}
