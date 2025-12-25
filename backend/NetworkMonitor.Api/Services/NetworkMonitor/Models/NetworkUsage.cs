// file: NetworkMonitor.Api/Services/NetworkMonitor/Models/NetworkUsage.cs
namespace NetworkMonitor.Api.Services.NetworkMonitor.Models;

public class NetworkUsage
{
    public long DownloadBytesPerSecond { get; set; }  // Receive
    public long UploadBytesPerSecond { get; set; }    // Send
}