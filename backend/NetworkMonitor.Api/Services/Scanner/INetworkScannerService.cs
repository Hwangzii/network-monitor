using NetworkMonitor.Api.DTOs;

namespace NetworkMonitor.Api.Services.Scanner;

public interface INetworkScannerService
{
    // Hàm này sẽ thực hiện quá trình quét và trả về danh sách thiết bị.
    Task<IEnumerable<NetworkDeviceDto>> ScanNetworkAsync();
}