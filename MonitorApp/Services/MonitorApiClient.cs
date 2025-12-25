using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MonitorApp.Models;   

namespace MonitorApp.Services
{
    public class MonitorApiClient
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public MonitorApiClient()
        {
            _http = new HttpClient
            {
                // đúng base URL backend của bạn
                BaseAddress = new Uri("http://localhost:5000/api/"),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        private async Task<T?> DeserializeAsync<T>(HttpResponseMessage resp)
        {
            resp.EnsureSuccessStatusCode();
            var stream = await resp.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions);
        }

        // ======================
        // TRAFFIC MONITOR
        // ======================

        public async Task<TrafficSummary?> GetTrafficSummaryAsync()
        {
            // KHÔNG dùng .Result để tránh deadlock
            var resp = await _http.GetAsync("traffic/summary");
            return await DeserializeAsync<TrafficSummary>(resp);
        }

        public async Task<TrafficChartResponse?> GetTrafficChartAsync(string range)
        {
            if (string.IsNullOrWhiteSpace(range))
                range = "5m";

            var resp = await _http.GetAsync($"traffic/chart?range={Uri.EscapeDataString(range)}");
            return await DeserializeAsync<TrafficChartResponse>(resp);
        }


        // ========== GRASSWIRE PROTECT / FIREWALL ==========

        public async Task<GrassWireProtectResponse?> GetFirewallAppsAsync(
            int page = 1, int limit = 20, string status = "active", bool includeProcess = true)
        {
            var query = $"firewall/apps?status={status}&includeProcess={includeProcess}&page={page}&limit={limit}";
            var resp = await _http.GetAsync(query);
            return await DeserializeAsync<GrassWireProtectResponse>(resp);
        }


        // ========== NETWORK SCANNER ==========

        public async Task<IReadOnlyList<ScannerDeviceDto>?> GetScannerDevicesAsync()
        {
            var resp = await _http.GetAsync("scanner/devices");
            return await DeserializeAsync<List<ScannerDeviceDto>>(resp);
        }

        public async Task<WifiInfoDto?> GetWifiInfoAsync()
        {
            var resp = await _http.GetAsync("scanner/wifi");
            return await DeserializeAsync<WifiInfoDto>(resp);
        }
    }
}
