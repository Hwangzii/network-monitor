using System.Collections.Generic;

namespace NetworkMonitor.Core.Utils
{
    public static class NormalizerService
    {
        private static readonly Dictionary<string, string> AdapterNameMap = new()
        {
            { "Ethernet", "network_adapter" },
            { "Wi-Fi", "network_adapter" },
            { "Local Area Connection", "network_adapter" },
            { "eth0", "network_adapter" },
            { "wlan0", "network_adapter" },
            // Thêm mapping khác nếu cần
        };

        private static readonly Dictionary<string, string> TrafficFieldMap = new()
        {
            { "BytesSent", "upload" },
            { "BytesReceived", "download" },
            { "tx_bytes", "upload" },
            { "rx_bytes", "download" },
            // Thêm nếu cần
        };

        public static string NormalizeAdapterName(string osSpecificName)
        {
            return AdapterNameMap.TryGetValue(osSpecificName, out var standard) ? standard : osSpecificName;
        }

        public static string NormalizeTrafficField(string osSpecificField)
        {
            return TrafficFieldMap.TryGetValue(osSpecificField, out var standard) ? standard : osSpecificField;
        }

        // Helper để lấy IP chuẩn (IPv4 prefer)
        public static string GetStandardIp(string[] addresses)
        {
            if (addresses == null || addresses.Length == 0) return "Unknown";
            foreach (var addr in addresses)
            {
                if (addr.Contains('.') && !addr.StartsWith("127.")) // IPv4 non-loopback
                {
                    return addr;
                }
            }
            return "Unknown";
        }
    }
}