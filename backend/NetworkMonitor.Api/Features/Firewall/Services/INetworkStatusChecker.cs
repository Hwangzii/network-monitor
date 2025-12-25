// file: NetworkMonitor.Api/Services/Firewall/INetworkStatusChecker.cs
namespace NetworkMonitor.Api.Services.Firewall;

public interface INetworkStatusChecker
{
    // Kiểm tra trạng thái cho một đường dẫn file .exe cụ thể
    string CheckInboundStatus(string executablePath);
    string CheckOutboundStatus(string executablePath);
}