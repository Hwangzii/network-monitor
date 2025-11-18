using MonitorApp.Models;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace MonitorApp.Services.Api
{
    public class ApiService
    {
        private readonly HttpClient _client;

        public ApiService()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://network-monitor-api.onrender.com/")
            };
        }

        public async Task<MonitorData> GetNetworkStatusAsync()
        {
            try
            {
                var response = await _client.GetAsync("monitor/status");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<MonitorData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Console.WriteLine($"[ApiService] ✅ IP: {data?.Ip}, Sent: {data?.BytesSent}, Received: {data?.BytesReceived}");
                return data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ API Error: {ex.Message}", "Network Monitor", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}
