using System.Text.Json.Serialization;
using NetworkMonitor.Api.DTOs;

namespace NetworkMonitor.Api.DTOs
{
    public class SystemInfoDto
    {
        [JsonPropertyName("machine_name")]
        public string MachineName { get; set; } = string.Empty;

        [JsonPropertyName("os")]
        public string Os { get; set; } = string.Empty;

        [JsonPropertyName("cpu_count")]
        public int CpuCount { get; set; }

        [JsonPropertyName("ram_available")]
        public long RamAvailable { get; set; } // bytes

        [JsonPropertyName("network_interfaces")]
        public List<NetworkInterfaceDto> NetworkInterfaces { get; set; } = new();
    }
}