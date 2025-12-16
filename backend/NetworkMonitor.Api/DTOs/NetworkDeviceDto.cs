// file: DTOs/DeviceResponseDto.cs
using System;

namespace NetworkMonitor.Api.DTOs;

public class NetworkDeviceResponseDto
{
    public bool isOnline { get; set; }
    public string type { get; set; } = "Generic";
    public string name { get; set; } = "Generic";
    public string description { get; set; } = "";
    public string location { get; set; } = "";
    public string system { get; set; } = "";
    public string ip { get; set; } = "";
    public string ports { get; set; } = "";
    public string mac_address { get; set; } = "";
    public string last_seen { get; set; } = "";
    public string first_seen { get; set; } = "";
}