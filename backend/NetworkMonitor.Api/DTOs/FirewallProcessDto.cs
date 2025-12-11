namespace NetworkMonitor.Api.DTOs;

public class FirewallProcessDto
{
    public string ProcessName { get; set; } = string.Empty;
    public int ProcessID { get; set; }
    public string IconUrl { get; set; } = string.Empty;
    public string InConnections { get; set; } = "";
    public string OutConnections { get; set; } = "";
    public string Hosts { get; set; } = "";
    public string DownloadSpeed { get; set; } = "0 B/s";
    public string UploadSpeed { get; set; } = "0 B/s";
    public string VirusTotal { get; set; } = "";
}