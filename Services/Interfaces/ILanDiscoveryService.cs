using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PottaKDS.Services.Interfaces
{
    public class DiscoveredServer
    {
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 5001;
        public string BaseUrl => $"http://{IpAddress}:{Port}";
        public string ServerName { get; set; } = "Potta POS Server";
        public bool IsLocalhost { get; set; }
    }

    public interface ILanDiscoveryService
    {
        Task<DiscoveredServer?> DiscoverServerAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);
        Task<List<DiscoveredServer>> ScanNetworkAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    }
}
