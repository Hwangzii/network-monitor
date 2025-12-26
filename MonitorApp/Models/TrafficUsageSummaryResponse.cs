using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MonitorApp.Models
{
    public class TrafficUsageSummaryResponse
    {
        public List<AppUsageDto> Apps { get; set; } = new();
        public List<HostUsageDto> Hosts { get; set; } = new();
        public List<TrafficTypeUsageDto> TrafficTypes { get; set; } = new();
        public List<CountryUsageDto> Countries { get; set; } = new();
    }

    public class AppUsageDto
    {
        public string? Name { get; set; }
        public string? Usage { get; set; }
        public long UsageBytes { get; set; }
        public string? CountryName { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryFlagUrl { get; set; }
        public string? AppIcon { get; set; } // data:image/png;base64,...
    }

    public class HostUsageDto
    {
        public string? Hostname { get; set; }
        public string? Usage { get; set; }
        public long UsageBytes { get; set; }
        public string? CountryName { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryFlagUrl { get; set; }
        public string? AppOwnerIcon { get; set; }
    }

    public class TrafficTypeUsageDto
    {
        public string? Type { get; set; }
        public string? Usage { get; set; }
        public double Percentage { get; set; } // backend đang trả kiểu “lạ”, vẫn nhận
    }

    public class CountryUsageDto
    {
        [JsonPropertyName("countryName")]
        public string? Country { get; set; }          // ✅ map đúng

        [JsonPropertyName("usage")]
        public string? Usage { get; set; }

        [JsonPropertyName("usageBytes")]
        public long UsageBytes { get; set; }

        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; set; }

        [JsonPropertyName("flagUrl")]
        public string? CountryFlagUrl { get; set; }   // ✅ map đúng
    }
}

