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
