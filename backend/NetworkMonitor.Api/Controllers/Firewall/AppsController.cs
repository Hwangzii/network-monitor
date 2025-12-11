using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Api.DTOs;

namespace NetworkMonitor.Api.Controllers.Firewall;

[ApiController]
[Route("api/firewall")]
[Produces("application/json")]
public class AppsController : ControllerBase
{
    private static readonly List<FirewallAppDto> MockData = new()
    {
        // === ACTIVE APPS ===
        new FirewallAppDto
        {
            AppName = "Microsoft Edge",
            IconUrl = "https://yourcdn.com/icons/ms_edge.png",
            InConnections = "Blocked",
            OutConnections = "Allowed",
            Version = "142.0.3935.94",
            Hosts = "onetouchcorpusa71.centralus.cloudapp.azure.com",
            DownloadSpeed = "94 B/s",
            UploadSpeed = "6 B/s",
            VirusTotal = "+6 more",
            Processes = new List<FirewallProcessDto>
            {
                new() { ProcessName = "msedge.exe", ProcessID = 16644, IconUrl = "https://yourcdn.com/icons/ms_edge.png", InConnections = "Blocked", OutConnections = "Allowed", Hosts = "onetouchcorpusa71.centralus.cloudapp.azure.com", DownloadSpeed = "94 B/s", UploadSpeed = "6 B/s", VirusTotal = "+6 more" },
                new() { ProcessName = "msedge.exe", ProcessID = 15616, IconUrl = "https://yourcdn.com/icons/ms_edge.png" },
                new() { ProcessName = "msedge.exe", ProcessID = 12212, IconUrl = "https://yourcdn.com/icons/ms_edge.png", Hosts = "192.168.1.252", VirusTotal = "+2 more" },
                new() { ProcessName = "msedge.exe", ProcessID = 1488,  IconUrl = "https://yourcdn.com/icons/ms_edge.png" }
            }
        },
        new FirewallAppDto
        {
            AppName = "Postman",
            IconUrl = "https://yourcdn.com/icons/postman.png",
            InConnections = "Blocked",
            OutConnections = "Allowed",
            Version = "11.7.5",
            Hosts = "clientstream-ga.launchdarkly.com",
            VirusTotal = "+5 more",
            Processes = new List<FirewallProcessDto>
            {
                new() { ProcessName = "postman.exe", ProcessID = 17328, IconUrl = "https://yourcdn.com/icons/postman.png", InConnections = "Blocked", OutConnections = "Allowed", Hosts = "clientstream-ga.launchdarkly.com", VirusTotal = "+5 more" },
                new() { ProcessName = "postman.exe", ProcessID = 1268,  IconUrl = "https://yourcdn.com/icons/postman.png", InConnections = "Blocked", OutConnections = "Allowed" }
            }
        },
        new FirewallAppDto { AppName = "Host Process for Windows Services", IconUrl = "https://yourcdn.com/icons/win_service_host.png", Version = "10.0.19041.5794", Hosts = "array16.prod.do.dsp.mp.microsoft.com", VirusTotal = "+4 more" },
        new FirewallAppDto { AppName = "Settings", IconUrl = "https://yourcdn.com/icons/windows_settings.png", Version = "10.0.19041.6496", Hosts = "e9813.cd.akamaiedge.net", VirusTotal = "+2 more" },
        new FirewallAppDto { AppName = "Visual Studio Code", IconUrl = "https://yourcdn.com/icons/vscode.png", Version = "1.106.3", Hosts = "ghs.github02crf3ed24d4.github.com", VirusTotal = "+1 more" },
        new FirewallAppDto { AppName = "AntiMalware Core Service", IconUrl = "https://yourcdn.com/icons/antimalware_core.png", Version = "4.18.23100.9008", Hosts = "f.v-0005.dual-g-msedge.net" },

        // === UNINSTALLED APPS ===
        new FirewallAppDto { AppName = "Dllm Host Servicing Process", IconUrl = "https://yourcdn.com/icons/generic_windows_process.png", Version = "10.0.19041.3626" },
        new FirewallAppDto { AppName = "Microsoft Malware Protection Signature Update Stub", IconUrl = "https://yourcdn.com/icons/ms_defender.png", Version = "1.1.2401.0.20001" },
        new FirewallAppDto { AppName = "Microsoft Visual C++ (x64) Redistributable", IconUrl = "https://yourcdn.com/icons/ms_visual_c++.png", Version = "14.39.32510" },
        new FirewallAppDto { AppName = "League of Legends", IconUrl = "https://yourcdn.com/icons/lol.png", Version = "15.24.786.7955" }
    };

    [HttpGet("apps")]
    public IActionResult GetFirewallApps()
    {
        var response = new FirewallAppsResponseDto
        {
            ActiveApps = MockData.Take(6).ToList(),
            UninstalledApps = MockData.Skip(6).ToList()
        };

        return Ok(response);
    }
}