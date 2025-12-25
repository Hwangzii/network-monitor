// file: NetworkMonitor.Api/Features/Traffic/DTOs/TrafficUsageSummaryDto.cs
// CẬP NHẬT: Sửa định nghĩa CountryUsageDto trong file này

namespace NetworkMonitor.Api.Features.Traffic.DTOs;

public class TrafficUsageSummaryDto
{
    public List<AppUsageDto> Apps { get; set; } = new();
    public List<HostUsageDto> Hosts { get; set; } = new();
    public List<TrafficTypeUsageDto> TrafficTypes { get; set; } = new();
    public List<CountryUsageDto> Countries { get; set; } = new();
}

public class AppUsageDto
{
    public string Name { get; set; } = string.Empty;
    public string Usage { get; set; } = "0 KB";
    public long UsageBytes { get; set; }
    public string CountryName { get; set; } = "Unknown";
    public string CountryCode { get; set; } = "un";
    public string CountryFlagUrl { get; set; } = "https://flagcdn.com/w20/un.png";
    public string? AppIcon { get; set; }
}

public class HostUsageDto
{
    public string Hostname { get; set; } = string.Empty;
    public string Usage { get; set; } = "0 KB";
    public long UsageBytes { get; set; }
    public string CountryName { get; set; } = "Unknown";
    public string CountryCode { get; set; } = "un";
    public string CountryFlagUrl { get; set; } = "https://flagcdn.com/w20/xx.png";
    public string? AppOwnerIcon { get; set; }
}

public class TrafficTypeUsageDto
{
    public string Type { get; set; } = "Unknown";
    public string Usage { get; set; } = "0 KB";
    public double Percentage { get; set; }
}

public class CountryUsageDto
{
    public string CountryName { get; set; } = "Unknown";
    public string CountryCode { get; set; } = "un";
    public string Usage { get; set; } = "0 KB";
    public string FlagUrl { get; set; } = "https://flagcdn.com/w40/un.png";
    
    // ✅ THÊM TRƯỜNG NÀY để hỗ trợ gộp dữ liệu lịch sử
    public long UsageBytes { get; set; }
}