using System.Text.Json.Serialization;

namespace MonitorApp.Models
{
    public class ScannerDeviceDto
    {
        [JsonPropertyName("isOnline")]
        public bool IsOnline { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("location")]
        public string Location { get; set; }

        [JsonPropertyName("system")]
        public string System { get; set; }

        [JsonPropertyName("ip")]
        public string Ip { get; set; }

        [JsonPropertyName("ports")]
        public string Ports { get; set; }

        [JsonPropertyName("mac_address")]
        public string Mac_Address { get; set; }

        [JsonPropertyName("last_seen")]
        public string Last_Seen { get; set; }

        [JsonPropertyName("first_seen")]
        public string First_Seen { get; set; }
    }

    public class WifiInfoDto
    {
        [JsonPropertyName("ssid")]
        public string Ssid { get; set; }

        [JsonPropertyName("bssid")]
        public string Bssid { get; set; }

        [JsonPropertyName("signalStrength")]
        public int SignalStrength { get; set; }

        [JsonPropertyName("signalBars")]
        public string SignalBars { get; set; }

        [JsonPropertyName("channel")]
        public int Channel { get; set; }

        [JsonPropertyName("security")]
        public string Security { get; set; }

        [JsonPropertyName("interfaceName")]
        public string InterfaceName { get; set; }

        [JsonPropertyName("isConnected")]
        public bool IsConnected { get; set; }
    }
}
