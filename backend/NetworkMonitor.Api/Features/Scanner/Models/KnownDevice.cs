// File: NetworkMonitor.Api/Models/KnownDevice.cs
using LiteDB;

namespace NetworkMonitor.Api.Models;

public class KnownDevice
{
    [BsonId]
    public string Mac { get; set; } = "";
    public string? CustomName { get; set; }
    public string Vendor { get; set; } = "";
    public string DeviceType { get; set; } = "Generic";
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public List<string> KnownIps { get; set; } = new();
    public List<string> KnownHostnames { get; set; } = new();
}