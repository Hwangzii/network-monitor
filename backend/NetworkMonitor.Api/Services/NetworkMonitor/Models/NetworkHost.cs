// file: NetworkMonitor.Api/Services/NetworkMonitor/Models/NetworkHost.cs
using System.Collections.Generic;

namespace NetworkMonitor.Api.Services.NetworkMonitor.Models;

public class NetworkHost
{
    public string RemoteIp { get; set; } = "";
    public string? Domain { get; set; }  // Bonus: có thể resolve sau
}

public class NetworkHostSet : HashSet<string> { }  // Để unique IP/domain