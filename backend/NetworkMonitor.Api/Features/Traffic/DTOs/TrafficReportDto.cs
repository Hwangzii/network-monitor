// NetworkMonitor.Api/Features/Traffic/DTOs/TrafficReportDto.cs
using System;
using System.Collections.Generic;
using NetworkMonitor.Api.DTOs;

namespace NetworkMonitor.Api.Features.Traffic.DTOs
{
    public class TrafficReportDto
    {
        public required TrafficSummaryDto Summary { get; set; }
        public required List<TrafficChartPointDto> Points { get; set; }
        public required PeakInfoDto Peak { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class PeakInfoDto
    {
        public double PeakDownload { get; set; }
        public double PeakUpload { get; set; }
        public DateTime Time { get; set; }
    }
}
