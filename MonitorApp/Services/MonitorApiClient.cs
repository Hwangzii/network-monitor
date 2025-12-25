using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MonitorApp.Models;

namespace MonitorApp.Services
{
    public class MonitorApiClient
    {
        // Reuse HttpClient để tránh socket exhaustion + lỗi lặt vặt khi gọi nhiều
        private static readonly HttpClient _http = CreateHttpClient();

        private static readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private static HttpClient CreateHttpClient()
        {
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
            };

            var http = new HttpClient(handler)
            {
                // đúng base URL backend của bạn
                BaseAddress = new Uri("http://localhost:5000/api/"),

                // FIX: tăng timeout (10s quá thấp nếu backend xử lý nặng)
                Timeout = TimeSpan.FromSeconds(60)
            };

            http.DefaultRequestHeaders.Accept.Clear();
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Debug.WriteLine($"🌐 MonitorApiClient BaseAddress = {http.BaseAddress}");
            Debug.WriteLine($"⏱ HttpClient Timeout = {http.Timeout.TotalSeconds}s");

            return http;
        }

        private static HttpRequestMessage BuildGet(string relativeUrl)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, relativeUrl);

            // FIX: ép HTTP/1.1 để né lỗi HTTP/2 "Response ended prematurely" (test rất hiệu quả)
            req.Version = HttpVersion.Version11;
            req.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            return req;
        }

        private static async Task<T?> SendAndDeserializeAsync<T>(string relativeUrl, CancellationToken ct = default)
        {
            try
            {
                using var req = BuildGet(relativeUrl);

                Debug.WriteLine($"➡️ GET {_http.BaseAddress}{relativeUrl}");

                // ResponseHeadersRead: nhận headers sớm, tránh treo do buffering lớn
                using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                Debug.WriteLine($"⬅️ {(int)resp.StatusCode} {resp.ReasonPhrase}");

                // Đọc body để debug (kể cả khi lỗi)
                var body = await resp.Content.ReadAsStringAsync(ct);
                Debug.WriteLine($"📦 Body(first 800) = {body.Substring(0, Math.Min(800, body.Length))}");

                resp.EnsureSuccessStatusCode();

                if (string.IsNullOrWhiteSpace(body))
                    return default;

                return JsonSerializer.Deserialize<T>(body, _jsonOptions);
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                // Timeout
                Debug.WriteLine($"💥 TIMEOUT after {_http.Timeout.TotalSeconds}s: {ex.Message}");
                return default;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"💥 HTTP ERROR: {ex}");
                return default;
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"💥 JSON PARSE ERROR: {ex}");
                return default;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 UNKNOWN ERROR: {ex}");
                return default;
            }
        }

        // ======================
        // TRAFFIC MONITOR
        // ======================

        public Task<TrafficSummary?> GetTrafficSummaryAsync(CancellationToken ct = default)
            => SendAndDeserializeAsync<TrafficSummary>("traffic/summary", ct);

        public Task<TrafficChartResponse?> GetTrafficChartAsync(string range, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(range)) range = "5m";
            return SendAndDeserializeAsync<TrafficChartResponse>($"traffic/chart?range={Uri.EscapeDataString(range)}", ct);
        }

        public Task<TrafficUsageSummaryResponse?> GetTrafficUsageSummaryAsync(CancellationToken ct = default)
            => SendAndDeserializeAsync<TrafficUsageSummaryResponse>("traffic/usage-summary", ct);

        // ========== GRASSWIRE PROTECT / FIREWALL ==========

        public Task<GrassWireProtectResponse?> GetFirewallAppsAsync(
            int page = 1, int limit = 20, string status = "active", bool includeProcess = true, CancellationToken ct = default)
        {
            var query = $"firewall/apps?status={Uri.EscapeDataString(status)}&includeProcess={includeProcess}&page={page}&limit={limit}";
            return SendAndDeserializeAsync<GrassWireProtectResponse>(query, ct);
        }

        // ========== NETWORK SCANNER ==========

        public async Task<IReadOnlyList<ScannerDeviceDto>?> GetScannerDevicesAsync(CancellationToken ct = default)
        {
            var list = await SendAndDeserializeAsync<List<ScannerDeviceDto>>("scanner/devices", ct);
            return list;
        }

        public Task<WifiInfoDto?> GetWifiInfoAsync(CancellationToken ct = default)
            => SendAndDeserializeAsync<WifiInfoDto>("scanner/wifi", ct);
    }
}
