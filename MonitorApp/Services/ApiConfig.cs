// Services/ApiConfig.cs
namespace MonitorApp.Services
{
    public static class ApiConfig
    {
        public static string BaseUrl { get; set; } = "http://localhost:5002/api/";

        // Dynamic range
        public static string GetTrafficSummaryEndpoint(string range = "5m")
            => $"traffic/summary?range={range}";

        public static string TrafficRealtime => "traffic/realtime";
        public static string NetworkStatus => "monitor/status";
    }
}