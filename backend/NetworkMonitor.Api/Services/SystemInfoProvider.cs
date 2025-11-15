// file: NetworkMonitor.Api/Services/SystemInfoProvider.cs
// Chỉ dùng trên Windows - Trả tên adapter thật, không chuẩn hóa.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using NetworkMonitor.Api.DTOs;
using System.Runtime.InteropServices;  

namespace NetworkMonitor.Api.Services
{
    public class SystemInfoProvider
    {
        public SystemInfoDto GetSystemInfo()
        {
            return new SystemInfoDto
            {
                MachineName = Environment.MachineName,
                Os = RuntimeInformation.OSDescription,
                CpuCount = Environment.ProcessorCount,
                RamAvailable = GetRamAvailable(),
                NetworkInterfaces = GetNetworkInterfaces()
            };
        }

        private long GetRamAvailable()
        {
            try
            {
                using var counter = new PerformanceCounter("Memory", "Available Bytes");
                return (long)counter.NextValue();
            }
            catch
            {
                return 0; // Cần chạy as Admin
            }
        }

        private List<NetworkInterfaceDto> GetNetworkInterfaces()
        {
            var interfaces = new List<NetworkInterfaceDto>();

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up 
                          && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            {
                var stats = nic.GetIPStatistics();
                var ip = GetFirstIPv4(nic.GetIPProperties().UnicastAddresses);

                interfaces.Add(new NetworkInterfaceDto
                {
                    NetworkAdapter = nic.Name,  // TÊN GỐC: "Wi-Fi", "Ethernet", v.v.
                    Ip = ip,
                    Upload = stats.BytesSent,
                    Download = stats.BytesReceived
                });
            }

            return interfaces;
        }

        // Helper: Lấy IPv4 đầu tiên (không loopback)
        private string GetFirstIPv4(IEnumerable<UnicastIPAddressInformation> addresses)
        {
            return addresses
                .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Select(a => a.Address.ToString())
                .FirstOrDefault(ip => !ip.StartsWith("127.")) ?? "Unknown";
        }
    }
}