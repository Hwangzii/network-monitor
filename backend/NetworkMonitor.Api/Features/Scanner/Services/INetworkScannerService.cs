// File: NetworkMonitor.Api/Services/Scanner/INetworkScannerService.cs

using NetworkMonitor.Api.DTOs;

namespace NetworkMonitor.Api.Services.Scanner;

public interface INetworkScannerService
{
    Task<IEnumerable<DeviceResponseDto>> ScanNetworkAsync();
}