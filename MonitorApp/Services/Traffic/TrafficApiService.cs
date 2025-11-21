// Services/Traffic/TrafficApiService.cs
using MonitorApp.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MonitorApp.Services.Traffic
{
    public class TrafficApiService
    {
        private readonly HttpClient _httpClient;

        public TrafficApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(ApiConfig.BaseUrl);
        }

        public async Task<TrafficSummary?> GetTrafficSummaryAsync(string range = "5m")
        {
            try
            {
                var endpoint = ApiConfig.GetTrafficSummaryEndpoint(range);
                var response = await _httpClient.GetAsync(endpoint);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<TrafficSummary>(json, options);
            }
            catch
            {
                return null;
            }
        }
    }
}