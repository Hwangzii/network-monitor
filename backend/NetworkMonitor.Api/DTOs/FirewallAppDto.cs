namespace NetworkMonitor.Api.DTOs;

public class FirewallAppDto
{
    public string AppName { get; set; } = string.Empty;
    public string IconBase64 { get; set; } = "";
    public string InConnections { get; set; } = "Allowed";
    public string OutConnections { get; set; } = "Allowed";
    public string Version { get; set; } = "";
    public string Hosts { get; set; } = "";
    public string VisualTotal { get; set; } = "";
    public string DownloadSpeed { get; set; } = "0 B/s";
    public string UploadSpeed { get; set; } = "0 B/s";
    public string VirusTotal { get; set; } = "";
    public List<FirewallProcessDto> Processes { get; set; } = new();
}