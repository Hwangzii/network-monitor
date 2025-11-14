using System.Text.Json.Serialization;

namespace NetworkMonitor.Api.DTOs
{
    public class NetworkInterfaceDto
    {
        [JsonPropertyName("network_adapter")]
        public string NetworkAdapter { get; set; } = string.Empty;

        [JsonPropertyName("ip")]
        public string Ip { get; set; } = string.Empty;

        [JsonPropertyName("upload")]
        public long Upload { get; set; } // bytes sent

        [JsonPropertyName("download")]
        public long Download { get; set; } // bytes received
    }
}