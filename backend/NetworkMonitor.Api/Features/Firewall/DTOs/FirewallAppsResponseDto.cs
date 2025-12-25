// file: NetworkMonitor.Api/DTOs/FirewallAppsResponseDto.cs
using System.Collections.Generic;

namespace NetworkMonitor.Api.DTOs;

public class FirewallAppsResponseDto
{
    public List<FirewallAppDto> ActiveApps { get; set; } = new();
    public List<FirewallAppDto> UninstalledApps { get; set; } = new();
}