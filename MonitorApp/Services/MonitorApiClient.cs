using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NetworkMonitor.Core.Models;

namespace MonitorApp.Services
{
    public class MonitorApiClient
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public MonitorApiClient(HttpClient httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
            // sửa BaseAddress cho trùng backend của bạn
            _http.BaseAddress = new Uri("https://localhost:5002/");
        }

        public async Task StartAsync()
        {
            var resp = await _http.PostAsync("monitor/start", content: null);
            resp.EnsureSuccessStatusCode();
        }

        public async Task StopAsync()
        {
            var resp = await _http.PostAsync("monitor/stop", content: null);
            resp.EnsureSuccessStatusCode();
        }

        public async Task<MonitorStatus?> GetStatusAsync()
        {
            var resp = await _http.GetAsync("monitor/status");
            resp.EnsureSuccessStatusCode();

            var stream = await resp.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<MonitorStatus>(stream, _jsonOptions);
        }
    }
}
