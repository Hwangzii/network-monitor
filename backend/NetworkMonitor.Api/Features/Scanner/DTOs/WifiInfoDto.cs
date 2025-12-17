// File: NetworkMonitor.Api/DTOs/WifiInfoDto.cs
namespace NetworkMonitor.Api.DTOs;

public class WifiInfoDto
{
    public string? Ssid { get; set; }           // Tên Wi-Fi (VD: "Hazii-Home")
    public string? Bssid { get; set; }          // MAC của Access Point
    public int SignalStrength { get; set; }     // % chất lượng tín hiệu (0-100)
    public string SignalBars => SignalStrength switch // Icon thanh sóng
    {
        >= 80 => "5 bars",
        >= 60 => "4 bars",
        >= 40 => "3 bars",
        >= 20 => "2 bars",
        _ => "1 bar"
    };
    public int Channel { get; set; }
    public string? Security { get; set; }       // WPA2, WPA3, Open...
    public string? InterfaceName { get; set; }  // Tên card Wi-Fi
    public bool IsConnected { get; set; } = true;
}