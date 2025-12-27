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
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;


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

        public async Task<byte[]?> GetTrafficExportPdfAsync(string range, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(range)) range = "5m";

            try
            {
                var url = $"traffic/export/pdf?range={Uri.EscapeDataString(range)}";

                using var req = BuildGet(url);
                Debug.WriteLine($"➡️ GET (PDF) {_http.BaseAddress}{url}");

                using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                Debug.WriteLine($"⬅️ (PDF) {(int)resp.StatusCode} {resp.ReasonPhrase}");

                resp.EnsureSuccessStatusCode();

                // đọc thẳng bytes (PDF)
                var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
                Debug.WriteLine($"📄 PDF bytes = {bytes?.Length ?? 0}");
                return bytes;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 EXPORT PDF ERROR: {ex}");
                return null;
            }
        }

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


        public async IAsyncEnumerable<SpeedEvent> SpeedRunStreamAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            using var req = BuildGet("speed/run");

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            // debug status
            System.Diagnostics.Debug.WriteLine($"[speed/run] {(int)resp.StatusCode} {resp.ReasonPhrase}");

            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                line = line.Trim();

                // SSE: "data: {...}"
                if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    line = line.Substring(5).Trim();

                // bỏ dòng không phải json
                if (!line.StartsWith("{")) continue;

                System.Diagnostics.Debug.WriteLine("[speed/run] " + line);

                SpeedEvent? ev = null;
                try
                {
                    ev = System.Text.Json.JsonSerializer.Deserialize<SpeedEvent>(line, _jsonOptions);
                }
                catch (System.Text.Json.JsonException jex)
                {
                    System.Diagnostics.Debug.WriteLine("[speed/run] JSON ERROR: " + jex.Message);
                    continue;
                }

                if (ev != null)
                    yield return ev;
            }
        }

        public Task<SpeedHistoryResponse?> GetSpeedHistoryAsync(int limit = 10, CancellationToken ct = default)
        {
            if (limit <= 0) limit = 10;
            return SendAndDeserializeAsync<SpeedHistoryResponse>($"speed/history?limit={limit}", ct);
        }

    }


}
