// file: NetworkMonitor.Api/Features/Traffic/Services/TrafficReportService.cs
using QuestPDF.Fluent;
using NetworkMonitor.Api.Features.Traffic.DTOs;
using NetworkMonitor.Api.Services.Traffic;

namespace NetworkMonitor.Api.Features.Traffic.Services
{
    public class TrafficReportService
    {
        private readonly TrafficService _trafficService;
        private readonly TrafficChartService _chartService;

        public TrafficReportService(TrafficService trafficService, TrafficChartService chartService)
        {
            _trafficService = trafficService;
            _chartService = chartService;
        }

        public async Task<byte[]> GenerateReportAsync(string range = "5m")
        {
            var summary = _trafficService.GetSummary();
            var chart = await _chartService.GetChartAsync(range);
            
            // Định nghĩa múi giờ Việt Nam
            var vietnamZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

            // Chuyển đổi dữ liệu sang giờ VN
            var points = chart.Points.Select(p => new TrafficChartPointDto 
            {
                Time = TimeZoneInfo.ConvertTimeFromUtc(p.Time.ToUniversalTime(), vietnamZone),
                Download = p.Download,
                Upload = p.Upload
            }).ToList();

            var peakPoint = points.OrderByDescending(p => p.Download + p.Upload).FirstOrDefault();

            var reportData = new TrafficReportDto
            {
                Summary = summary,
                Points = points,
                Peak = new PeakInfoDto
                {
                    PeakDownload = peakPoint?.Download ?? 0,
                    PeakUpload = peakPoint?.Upload ?? 0,
                    Time = peakPoint?.Time ?? DateTime.Now // Đã là giờ VN
                },
                From = points.FirstOrDefault()?.Time ?? DateTime.Now,
                To = points.LastOrDefault()?.Time ?? DateTime.Now
            };

            var document = new TrafficReportDocument(reportData);
            return document.GeneratePdf();
        }
    }
}