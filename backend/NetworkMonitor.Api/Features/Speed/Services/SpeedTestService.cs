// Features/Speed/Services/SpeedTestService.cs
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using NetworkMonitor.Api.Features.Speed.DTOs;
using NetworkMonitor.Api.Features.Speed.Models;

namespace NetworkMonitor.Api.Features.Speed.Services;

public class SpeedTestService
{
    private static readonly HttpClient _httpClient = new HttpClient();

    private readonly SpeedTestHistoryService _historyService; // <-- THÊM

    public SpeedTestService(SpeedTestHistoryService historyService)
    {
        _historyService = historyService; // <-- Inject để lưu lịch sử
    }

    public delegate Task ProgressCallback(string type, object data);

    public async Task ExecuteSpeedTestStreamingAsync(ProgressCallback callback)
    {
        await Task.Run(async () =>
        {
            try
            {
                string exePath = Path.Combine(AppContext.BaseDirectory, SpeedTestConstants.RelativeExePath);

                if (!File.Exists(exePath))
                {
                    await callback("error", "speedtest.exe not found. Please rebuild the project.");
                    return;
                }

                await callback("status", SpeedTestConstants.StatusMessages.Initializing);
                await Task.Delay(1000);

                await callback("status", SpeedTestConstants.StatusMessages.FindingServer);
                await Task.Delay(2500);

                await callback("status", SpeedTestConstants.StatusMessages.MeasuringPing);
                await Task.Delay(2000);

                await callback("status", SpeedTestConstants.StatusMessages.TestingBandwidth);

                var process = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "--format=json --accept-license --accept-gdpr",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(process);
                if (proc == null) throw new InvalidOperationException("Failed to start speedtest process.");

                string output = await proc.StandardOutput.ReadToEndAsync();
                string error = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();

                if (proc.ExitCode != 0)
                {
                    await callback("error", $"Speedtest failed: {error.Trim()}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    await callback("error", "Empty result from speedtest.");
                    return;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<SpeedTestCliResult>(output, options);

                if (result == null)
                {
                    await callback("error", "Failed to parse speedtest result.");
                    return;
                }

                double pingMs = result.Ping.Latency;
                double downloadMbps = Math.Round(result.Download.Bandwidth / 125000.0, 2);
                double uploadMbps = Math.Round(result.Upload.Bandwidth / 125000.0, 2);

                string ipv4Address = "Unknown";
                try
                {
                    ipv4Address = (await _httpClient.GetStringAsync("https://api.ipify.org")).Trim();
                }
                catch { /* ignore */ }

                await callback("status", SpeedTestConstants.StatusMessages.TestCompleted);
                await Task.Delay(800);

                await callback("ping", new
                {
                    pingMs,
                    quality = SpeedTestQualityEvaluator.GetPingQuality(pingMs)
                });
                await Task.Delay(400);

                await callback("download", new
                {
                    downloadMbps,
                    quality = SpeedTestQualityEvaluator.GetDownloadQuality(downloadMbps)
                });
                await Task.Delay(400);

                await callback("upload", new
                {
                    uploadMbps,
                    quality = SpeedTestQualityEvaluator.GetUploadQuality(uploadMbps)
                });
                await Task.Delay(400);

                var finalResult = new SpeedTestResponseDto
                {
                    DownloadMbps = downloadMbps,
                    UploadMbps = uploadMbps,
                    PingMs = pingMs,
                    IpAddress = ipv4Address,
                    Provider = result.Isp,
                    Location = "Vietnam"
                };

                await callback("complete", finalResult);

                // === LƯU KẾT QUẢ VÀO CƠ SỞ DỮ LIỆU SQLITE ===
                await _historyService.AddResultAsync(downloadMbps, uploadMbps, pingMs);
            }
            catch (Exception ex)
            {
                await callback("error", $"Ookla SpeedTest Error: {ex.Message}");
            }
        });
    }
}