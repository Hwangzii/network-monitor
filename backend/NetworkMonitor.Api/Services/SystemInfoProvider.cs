// file: NetworkMonitor.Api/Services/SystemInfoProvider.cs

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NetworkMonitor.Api.DTOs;
using NetworkMonitor.Core.Utils;

namespace NetworkMonitor.Api.Services
{
    public class SystemInfoProvider
    {
        public SystemInfoDto GetSystemInfo()
        {
            var dto = new SystemInfoDto
            {
                MachineName = Environment.MachineName,
                Os = RuntimeInformation.OSDescription, // Cross-platform: "Microsoft Windows 10.0.22621" hoặc "Ubuntu 22.04"
                CpuCount = Environment.ProcessorCount,
                RamAvailable = GetRamAvailable(),
                NetworkInterfaces = GetNetworkInterfaces()
            };

            return dto;
        }

        private long GetRamAvailable()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: PerformanceCounter (guarded by runtime + compile-time #if)
#if WINDOWS
                try
                {
                    using var counter = new PerformanceCounter("Memory", "Available Bytes");
                    return (long)counter.NextValue();
                }
                catch
                {
                    // Fallback nếu counter lỗi
                    return 0;
                }
#else
                return 0;
#endif
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: Parse /proc/meminfo MemAvailable (cải thiện parse cho Ubuntu 24.04)
                try
                {
                    var memInfo = File.ReadAllText("/proc/meminfo");
                    var lines = memInfo.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var memLine = lines.FirstOrDefault(l => l.StartsWith("MemAvailable:", StringComparison.OrdinalIgnoreCase));
                    if (memLine != null)
                    {
                        var parts = memLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 1 && long.TryParse(parts[1], out var value))
                        {
                            return value * 1024; // kB to bytes
                        }
                    }
                    // Fallback: Tính MemFree + Cached + Buffers (an toàn nếu MemAvailable thiếu)
                    long free = 0, cached = 0, buffers = 0;
                    var freeLine = lines.FirstOrDefault(l => l.StartsWith("MemFree:", StringComparison.OrdinalIgnoreCase));
                    var cachedLine = lines.FirstOrDefault(l => l.StartsWith("Cached:", StringComparison.OrdinalIgnoreCase));
                    var buffersLine = lines.FirstOrDefault(l => l.StartsWith("Buffers:", StringComparison.OrdinalIgnoreCase));
                    if (freeLine != null) 
                    { 
                        var p = freeLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries); 
                        long.TryParse(p.Length > 1 ? p[1] : "0", out free); 
                    }
                    if (cachedLine != null) 
                    { 
                        var p = cachedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries); 
                        long.TryParse(p.Length > 1 ? p[1] : "0", out cached); 
                    }
                    if (buffersLine != null) 
                    { 
                        var p = buffersLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries); 
                        long.TryParse(p.Length > 1 ? p[1] : "0", out buffers); 
                    }
                    return (free + cached + buffers) * 1024;
                }
                catch
                {
                    return 0;
                }
            }
            return 0;
        }

        private List<NetworkInterfaceDto> GetNetworkInterfaces()
        {
            var interfaces = new List<NetworkInterfaceDto>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: System.Net.NetworkInformation
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback))
                {
                    var stats = nic.GetIPStatistics();
                    var addresses = nic.GetIPProperties().UnicastAddresses
                        .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Select(a => a.Address.ToString()).ToArray();

                    interfaces.Add(new NetworkInterfaceDto
                    {
                        NetworkAdapter = NormalizerService.NormalizeAdapterName(nic.Name),
                        Ip = NormalizerService.GetStandardIp(addresses),
                        Upload = stats.BytesSent,
                        Download = stats.BytesReceived
                    });
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: Parse /proc/net/dev cho traffic, ip addr cho IP (thêm filter bỏ "lo")
                try
                {
                    // Traffic from /proc/net/dev
                    var devContent = File.ReadAllText("/proc/net/dev");
                    var lines = devContent.Split('\n').Skip(2); // Skip header

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split(new[] { ':' }, 2);
                        if (parts.Length != 2) continue;

                        var ifaceName = parts[0].Trim();
                        if (ifaceName == "lo") continue;  // THÊM: Filter loopback như Windows

                        var stats = parts[1].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (stats.Length < 9) continue; // rx_bytes (1), tx_bytes (9)

                        var rxBytes = long.Parse(stats[1]); // download
                        var txBytes = long.Parse(stats[9]); // upload

                        // IP: Run "ip addr show <iface>" và parse inet
                        var ipProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "ip",
                                Arguments = $"addr show {ifaceName}",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        ipProcess.Start();
                        var ipOutput = ipProcess.StandardOutput.ReadToEnd();
                        ipProcess.WaitForExit();

                        var ipMatch = Regex.Match(ipOutput, @"inet (\d+\.\d+\.\d+\.\d+)/");
                        var ip = ipMatch.Success ? ipMatch.Groups[1].Value : "Unknown";

                        interfaces.Add(new NetworkInterfaceDto
                        {
                            NetworkAdapter = NormalizerService.NormalizeAdapterName(ifaceName),
                            Ip = ip,
                            Upload = txBytes,
                            Download = rxBytes
                        });
                    }
                }
                catch
                {
                    // Fallback empty
                }
            }

            return interfaces;
        }
    }
}