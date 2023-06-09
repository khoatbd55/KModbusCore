using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KModbus.IO
{
    public interface IModbusChannel
    {
        string Endpoint { get; }
        bool IsSecureConnection { get; }
        X509Certificate2 ClientCertificate { get; }

        Task ConnectAsync(CancellationToken cancellationToken);
        void DisconnectAsync();

        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
    }
}
