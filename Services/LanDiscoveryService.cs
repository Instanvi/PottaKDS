using PottaKDS.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PottaKDS.Services
{
    public class LanDiscoveryService : ILanDiscoveryService
    {
        private static readonly HttpClient _probeClient = new()
        {
            Timeout = TimeSpan.FromMilliseconds(1200)
        };

        public async Task<DiscoveredServer?> DiscoverServerAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            progress?.Report("Checking localhost (Port 5001)...");

            // 1. Check localhost first
            if (await TestIpAsync("localhost", 5001, cancellationToken))
            {
                return new DiscoveredServer
                {
                    IpAddress = "localhost",
                    Port = 5001,
                    IsLocalhost = true,
                    ServerName = "Local Computer"
                };
            }

            // 2. Check 127.0.0.1
            if (await TestIpAsync("127.0.0.1", 5001, cancellationToken))
            {
                return new DiscoveredServer
                {
                    IpAddress = "127.0.0.1",
                    Port = 5001,
                    IsLocalhost = true,
                    ServerName = "Localhost"
                };
            }

            // 3. Scan Local Network Subnet
            progress?.Report("Scanning local network for Potta POS...");
            var servers = await ScanNetworkAsync(progress, cancellationToken);
            return servers.FirstOrDefault();
        }

        public async Task<List<DiscoveredServer>> ScanNetworkAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            var discovered = new ConcurrentBag<DiscoveredServer>();
            var localIps = GetLocalIpv4Addresses();

            if (!localIps.Any())
            {
                progress?.Report("No active network adapter found.");
                return discovered.ToList();
            }

            foreach (var localIp in localIps)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var ipParts = localIp.Split('.');
                if (ipParts.Length != 4) continue;

                var subnetPrefix = $"{ipParts[0]}.{ipParts[1]}.{ipParts[2]}";
                progress?.Report($"Scanning subnet {subnetPrefix}.1-254...");

                // Probe hosts in parallel batches with controlled throttle
                var tasks = new List<Task>();
                var semaphore = new SemaphoreSlim(30); // 30 concurrent socket/HTTP probes

                for (int i = 1; i <= 254; i++)
                {
                    var targetIp = $"{subnetPrefix}.{i}";
                    tasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            if (cancellationToken.IsCancellationRequested) return;

                            if (await TestIpAsync(targetIp, 5001, cancellationToken))
                            {
                                discovered.Add(new DiscoveredServer
                                {
                                    IpAddress = targetIp,
                                    Port = 5001,
                                    IsLocalhost = (targetIp == localIp),
                                    ServerName = $"Potta POS ({targetIp})"
                                });
                            }
                        }
                        catch
                        {
                            // Ignore probe errors
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken));
                }

                await Task.WhenAll(tasks);
            }

            progress?.Report($"Scan finished. Found {discovered.Count} server(s).");
            return discovered.ToList();
        }

        private async Task<bool> TestIpAsync(string ip, int port, CancellationToken cancellationToken)
        {
            try
            {
                var url = $"http://{ip}:{port}/health";
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMilliseconds(1000));

                var response = await _probeClient.GetAsync(url, cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private List<string> GetLocalIpv4Addresses()
        {
            var addresses = new List<string>();
            try
            {
                foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (netInterface.OperationalStatus == OperationalStatus.Up &&
                        netInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        var ipProps = netInterface.GetIPProperties();
                        foreach (var addr in ipProps.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                addresses.Add(addr.Address.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving IP addresses: {ex.Message}");
            }

            return addresses;
        }
    }
}
