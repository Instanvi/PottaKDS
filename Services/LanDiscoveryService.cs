using PottaKDS.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PottaKDS.Services
{
    public class LanDiscoveryService : ILanDiscoveryService
    {
        private static readonly HttpClient _probeClient = new()
        {
            Timeout = TimeSpan.FromMilliseconds(1500)
        };

        public Task<DiscoveredServer?> DiscoverServerAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            return DiscoverServerAsync(5001, progress, cancellationToken);
        }

        public async Task<DiscoveredServer?> DiscoverServerAsync(int port, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            // 1. Scan network FIRST to find actual IP addresses across the LAN
            progress?.Report("Scanning local network for Potta POS...");
            var servers = await ScanNetworkAsync(port, progress, cancellationToken);

            if (servers.Count > 0)
            {
                progress?.Report($"Found {servers.Count} server(s) on network.");
                return servers.First();
            }

            // 2. Fallback to localhost ONLY if network scan found nothing (e.g. offline standalone)
            progress?.Report("Network scan found nothing. Checking localhost fallback...");

            if (await IsPortOpenAsync("127.0.0.1", port, 500, cancellationToken))
            {
                var verified = await VerifyPottaServerAsync("127.0.0.1", port, isLocalHost: true, cancellationToken);
                if (verified != null)
                {
                    progress?.Report($"Found server: 127.0.0.1:{port}");
                    return verified;
                }
            }

            if (await IsPortOpenAsync("localhost", port, 500, cancellationToken))
            {
                var verified = await VerifyPottaServerAsync("localhost", port, isLocalHost: true, cancellationToken);
                if (verified != null)
                {
                    progress?.Report($"Found server: localhost:{port}");
                    return verified;
                }
            }

            progress?.Report("No Potta POS servers found.");
            return null;
        }

        public Task<List<DiscoveredServer>> ScanNetworkAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            return ScanNetworkAsync(5001, progress, cancellationToken);
        }

        public async Task<List<DiscoveredServer>> ScanNetworkAsync(int port, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            var discovered = new ConcurrentBag<DiscoveredServer>();
            var (subnets, localIps) = GetLocalSubnetsAndIps();

            if (subnets.Count == 0)
            {
                progress?.Report("No active network adapter found.");
                return discovered.ToList();
            }

            progress?.Report($"Scanning {subnets.Count} network subnet(s)...");

            foreach (var subnet in subnets)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var subnetPrefix = subnet.Prefix;
                progress?.Report($"Scanning subnet {subnetPrefix}.0/24...");

                // Prioritize this PC's own local IP on this subnet if present
                if (!string.IsNullOrEmpty(subnet.LocalIp))
                {
                    if (await IsPortOpenAsync(subnet.LocalIp, port, 400, cancellationToken))
                    {
                        var localServer = await VerifyPottaServerAsync(subnet.LocalIp, port, isLocalHost: true, cancellationToken);
                        if (localServer != null && !discovered.Any(s => s.IpAddress == subnet.LocalIp))
                        {
                            discovered.Add(localServer);
                            progress?.Report($"Found server on this PC: {subnet.LocalIp}:{port}");
                        }
                    }
                }

                // Concurrently probe all 254 host IPs on this subnet
                var tasks = new List<Task>();
                var semaphore = new SemaphoreSlim(60); // 60 parallel TCP probes
                int progressCounter = 0;

                for (int i = 1; i <= 254; i++)
                {
                    var targetIp = $"{subnetPrefix}.{i}";

                    // If already discovered as local IP, skip duplicate
                    if (discovered.Any(s => s.IpAddress == targetIp))
                        continue;

                    tasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            if (cancellationToken.IsCancellationRequested) return;

                            // Fast TCP connect probe without ping
                            if (await IsPortOpenAsync(targetIp, port, 450, cancellationToken))
                            {
                                bool isThisPc = localIps.Contains(targetIp);
                                var server = await VerifyPottaServerAsync(targetIp, port, isThisPc, cancellationToken);
                                if (server != null)
                                {
                                    discovered.Add(server);
                                    progress?.Report($"Found server: {targetIp}:{port}");
                                }
                            }

                            var current = Interlocked.Increment(ref progressCounter);
                            if (current % 30 == 0 || current == 254)
                            {
                                var percent = (current * 100) / 254;
                                progress?.Report($"Scanning subnet: {percent}% ({current}/254 checked)");
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

            // Order servers: Put servers on the network first, then this PC
            var serverList = discovered
                .OrderBy(s => s.IsLocalhost ? 1 : 0)
                .ThenBy(s => s.IpAddress)
                .ToList();

            if (serverList.Count > 0)
            {
                progress?.Report($"Scan complete: Found {serverList.Count} server(s).");
            }
            else
            {
                progress?.Report($"Scan complete: No servers found on {subnets.Count} subnet(s).");
            }

            return serverList;
        }

        /// <summary>
        /// Direct fast TCP socket probe without relying on ICMP Ping (which is blocked by Windows Firewall).
        /// </summary>
        private static async Task<bool> IsPortOpenAsync(string ip, int port, int timeoutMs, CancellationToken cancellationToken)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true
                };

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeoutMs);

                await socket.ConnectAsync(ip, port, cts.Token);
                return socket.Connected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifies whether the open port is actually Potta POS by checking endpoints and resolving server name.
        /// </summary>
        private static async Task<DiscoveredServer?> VerifyPottaServerAsync(string ip, int port, bool isLocalHost, CancellationToken cancellationToken)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMilliseconds(1200));

                string resolvedName = isLocalHost ? $"Potta POS (This PC - {ip})" : $"Potta POS ({ip})";

                // 1. Try querying /api/network/qr-string to read actual machine name
                try
                {
                    var qrUrl = $"http://{ip}:{port}/api/network/qr-string";
                    var qrResponse = await _probeClient.GetAsync(qrUrl, cts.Token);
                    if (qrResponse.IsSuccessStatusCode)
                    {
                        var content = await qrResponse.Content.ReadAsStringAsync(cts.Token);
                        using var doc = JsonDocument.Parse(content);
                        if (doc.RootElement.TryGetProperty("data", out var dataEl) &&
                            dataEl.TryGetProperty("serverName", out var nameEl))
                        {
                            var machineName = nameEl.GetString();
                            if (!string.IsNullOrWhiteSpace(machineName))
                            {
                                resolvedName = isLocalHost ? $"{machineName} (This PC)" : machineName;
                            }
                        }

                        return new DiscoveredServer
                        {
                            IpAddress = ip,
                            Port = port,
                            IsLocalhost = isLocalHost,
                            ServerName = resolvedName
                        };
                    }
                }
                catch
                {
                    // Fall back to health check
                }

                // 2. Try /health
                var healthUrl = $"http://{ip}:{port}/health";
                var response = await _probeClient.GetAsync(healthUrl, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    return new DiscoveredServer
                    {
                        IpAddress = ip,
                        Port = port,
                        IsLocalhost = isLocalHost,
                        ServerName = resolvedName
                    };
                }

                // 3. Try /api/orders/waiting
                var ordersUrl = $"http://{ip}:{port}/api/orders/waiting";
                var ordersResponse = await _probeClient.GetAsync(ordersUrl, cts.Token);
                if (ordersResponse.IsSuccessStatusCode)
                {
                    return new DiscoveredServer
                    {
                        IpAddress = ip,
                        Port = port,
                        IsLocalhost = isLocalHost,
                        ServerName = resolvedName
                    };
                }
            }
            catch
            {
                // Not Potta POS or probe failed
            }

            return null;
        }

        private class SubnetInfo
        {
            public string Prefix { get; set; } = string.Empty;
            public bool HasGateway { get; set; }
            public string LocalIp { get; set; } = string.Empty;
        }

        private static (List<SubnetInfo> Subnets, HashSet<string> LocalIps) GetLocalSubnetsAndIps()
        {
            var subnets = new List<SubnetInfo>();
            var localIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenPrefixes = new HashSet<string>();

            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                 ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                 ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .ToList();

                // Sort so interfaces with Default Gateway (connected to active LAN router/switch) come first
                var orderedInterfaces = interfaces
                    .OrderByDescending(ni => ni.GetIPProperties().GatewayAddresses.Any(g =>
                        g.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !g.Address.Equals(IPAddress.Any) &&
                        !g.Address.ToString().StartsWith("127.")))
                    .ToList();

                foreach (var ni in orderedInterfaces)
                {
                    var ipProps = ni.GetIPProperties();
                    var hasGateway = ipProps.GatewayAddresses.Any(g =>
                        g.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !g.Address.Equals(IPAddress.Any) &&
                        !g.Address.ToString().StartsWith("127."));

                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily != AddressFamily.InterNetwork)
                            continue;

                        var ipStr = addr.Address.ToString();
                        if (ipStr.StartsWith("127.") || ipStr.StartsWith("169.254."))
                            continue;

                        localIps.Add(ipStr);

                        var parts = ipStr.Split('.');
                        if (parts.Length == 4)
                        {
                            var prefix = $"{parts[0]}.{parts[1]}.{parts[2]}";
                            if (!seenPrefixes.Contains(prefix))
                            {
                                seenPrefixes.Add(prefix);
                                subnets.Add(new SubnetInfo
                                {
                                    Prefix = prefix,
                                    HasGateway = hasGateway,
                                    LocalIp = ipStr
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving IP addresses: {ex.Message}");
            }

            return (subnets, localIps);
        }
    }
}
