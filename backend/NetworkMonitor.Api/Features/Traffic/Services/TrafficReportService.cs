// NetworkMonitor.Api/Features/Traffic/Services/TrafficReportService.cs
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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

            // Tìm peak
            var peakPoint = chart.Points.OrderByDescending(p => p.Download + p.Upload).FirstOrDefault();

            var report = new TrafficReportDto
            {
                Summary = summary,
                Points = chart.Points,
                Peak = new PeakInfoDto
                {
                    PeakDownload = peakPoint?.Download ?? 0,
                    PeakUpload = peakPoint?.Upload ?? 0,
                    Time = peakPoint?.Time ?? DateTime.UtcNow
                },
                From = chart.Points.FirstOrDefault()?.Time ?? DateTime.UtcNow,
                To = chart.Points.LastOrDefault()?.Time ?? DateTime.UtcNow
            };

            // ===== TẠO PDF =====
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Báo cáo giám sát mạng")
                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(10)
                        .Column(col =>
                        {
                            col.Item().Text($"Thời gian: {report.From:HH:mm:ss} – {report.To:HH:mm:ss} UTC");

                            col.Item().Text("=== TỔNG QUAN ===").Bold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(150); // cột tên
                                    columns.RelativeColumn();    // cột giá trị
                                });

                                void AddRow(string name, string value)
                                {
                                    table.Cell().Text(name).SemiBold(); // ô 1
                                    table.Cell().Text(value);           // ô 2
                                }

                                AddRow("Tổng lưu lượng", summary.TotalUsage);
                                AddRow("Download", summary.DownloadTotal);
                                AddRow("Upload", summary.UploadTotal);
                                AddRow("WAN", summary.WanUsage);
                                AddRow("LAN", summary.LanUsage);
                                AddRow("Download Ratio", summary.DownloadRatio + "%");
                                AddRow("Upload Ratio", summary.UploadRatio + "%");
                            });

                            col.Item().Text("=== Peak ===").Bold();
                            col.Item().Text($"Download: {report.Peak.PeakDownload:0.##} B/s");
                            col.Item().Text($"Upload: {report.Peak.PeakUpload:0.##} B/s");
                            col.Item().Text($"Thời điểm: {report.Peak.Time:HH:mm:ss}");

                            col.Item().Text("=== Biến động theo thời gian (Peak 20s) ===").Bold();

                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(); // Thời gian
                                    c.RelativeColumn(); // Download
                                    c.RelativeColumn(); // Upload
                                });

                                t.Header(header =>
                                {
                                    header.Cell().Text("Thời gian").SemiBold();
                                    header.Cell().Text("Download").SemiBold();
                                    header.Cell().Text("Upload").SemiBold();
                                });

                                foreach (var point in report.Points)
                                {
                                    t.Cell().Text(point.Time.ToString("HH:mm:ss"));
                                    t.Cell().Text(point.Download.ToString("0.##"));
                                    t.Cell().Text(point.Upload.ToString("0.##"));
                                }
                            });

                        });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }
    }
}
