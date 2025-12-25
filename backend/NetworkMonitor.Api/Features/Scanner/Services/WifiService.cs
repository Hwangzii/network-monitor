// File: Services/Scanner/WifiService.cs
using NetworkMonitor.Api.DTOs;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace NetworkMonitor.Api.Services.Scanner;

public interface IWifiService
{
    Task<WifiInfoDto> GetCurrentWifiAsync();
}

public class WifiService : IWifiService
{
    private readonly ILogger<WifiService> _logger;

    public WifiService(ILogger<WifiService> logger)
    {
        _logger = logger;
    }

    public async Task<WifiInfoDto> GetCurrentWifiAsync()
    {
        return await Task.Run(() =>
        {
            if (!OperatingSystem.IsWindows())
            {
                return new WifiInfoDto
                {
                    IsConnected = false,
                    Ssid = "Only supported on Windows"
                };
            }

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "wlan show interfaces",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(8000);

                var dto = new WifiInfoDto();

                foreach (var line in output.Split('\n'))
                {
                    var trimmed = line.Trim();

                    if (trimmed.StartsWith("SSID") && !trimmed.Contains("BSSID"))
                        dto.Ssid = trimmed["SSID".Length..].TrimStart(':', ' ').Trim();

                    else if (trimmed.StartsWith("BSSID"))
                        dto.Bssid = trimmed["BSSID".Length..].TrimStart(':', ' ').Trim();

                    else if (trimmed.StartsWith("Signal"))
                    {
                        var match = Regex.Match(trimmed, @"\d+");
                        if (match.Success && int.TryParse(match.Value, out int signal))
                            dto.SignalStrength = signal;
                    }

                    else if (trimmed.StartsWith("Channel"))
                    {
                        var match = Regex.Match(trimmed, @"\d+");
                        if (match.Success && int.TryParse(match.Value, out int ch))
                            dto.Channel = ch;
                    }

                    else if (trimmed.StartsWith("Authentication"))
                        dto.Security = trimmed.Contains("WPA3") ? "WPA3" :
                                      trimmed.Contains("WPA2") ? "WPA2" :
                                      trimmed.Contains("WPA") ? "WPA" : "Open";

                    else if (trimmed.StartsWith("Name"))
                        dto.InterfaceName = trimmed["Name".Length..].TrimStart(':', ' ').Trim();
                }

                dto.IsConnected = !string.IsNullOrWhiteSpace(dto.Ssid) && 
                                 dto.Ssid != "Not connected" && 
                                 dto.SignalStrength > 0;

                if (!dto.IsConnected)
                    dto.Ssid = "Not connected";

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve Wi-Fi information");
                return new WifiInfoDto
                {
                    IsConnected = false,
                    Ssid = "Error reading Wi-Fi"
                };
            }
        });
    }
}