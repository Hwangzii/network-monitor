using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using NetworkMonitor.Api.Features.Traffic.DTOs;

namespace NetworkMonitor.Api.Features.Traffic.Services
{
    public class TrafficReportDocument : IDocument
    {
        private readonly TrafficReportDto _data;

        public TrafficReportDocument(TrafficReportDto data)
        {
            _data = data;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                
                // --- THAY ĐỔI: Tăng LineHeight lên 1.5 để các dòng văn bản thưa ra ---
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana).LineHeight(1.5f));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Trang ");
                    x.CurrentPageNumber();
                });
            });
        }

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("BÁO CÁO GIÁM SÁT LƯU LƯỢNG MẠNG")
                        .FontSize(18).SemiBold().FontColor(Colors.Blue.Medium);
                    
                    col.Item().Text(text =>
                    {
                        text.Span("Khoảng thời gian: ").SemiBold();
                        // Hiển thị text rõ ràng là giờ Việt Nam
                        text.Span($"{_data.From:HH:mm:ss} - {_data.To:HH:mm:ss} (Giờ Việt Nam)");
                    });
                });

                row.ConstantItem(120).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(9);
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(15).Column(column =>
            {
                column.Item().PaddingBottom(5).Text("1. Tổng quan hệ thống").FontSize(12).SemiBold().Underline();
                
                // Tăng PaddingBottom để các phần tách biệt nhau hơn
                column.Item().PaddingBottom(20).Row(row =>
                {
                    row.RelativeItem().Element(CardStyle).Column(c => {
                        c.Item().AlignCenter().Text("TỔNG LƯU LƯỢNG").FontSize(8).HeaderStyle();
                        c.Item().AlignCenter().PaddingTop(2).Text(_data.Summary.TotalUsage).FontSize(12).Bold();
                    });
                    row.ConstantItem(15); // Tăng khoảng cách giữa các card
                    row.RelativeItem().Element(CardStyle).Column(c => {
                        c.Item().AlignCenter().Text("DOWNLOAD").FontSize(8).HeaderStyle();
                        c.Item().AlignCenter().PaddingTop(2).Text(_data.Summary.DownloadTotal).FontSize(12).Bold().FontColor(Colors.Green.Medium);
                    });
                    row.ConstantItem(15);
                    row.RelativeItem().Element(CardStyle).Column(c => {
                        c.Item().AlignCenter().Text("UPLOAD").FontSize(8).HeaderStyle();
                        c.Item().AlignCenter().PaddingTop(2).Text(_data.Summary.UploadTotal).FontSize(12).Bold().FontColor(Colors.Orange.Medium);
                    });
                });

                column.Item().PaddingBottom(5).Text("2. Chỉ số cao điểm (Peak)").FontSize(12).SemiBold().Underline();
                column.Item().PaddingBottom(20).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(12).Row(row =>
                {
                    row.RelativeItem().Text(t => { t.Span("Peak DL: ").SemiBold(); t.Span($"{_data.Peak.PeakDownload:N0} B/s"); });
                    row.RelativeItem().Text(t => { t.Span("Peak UL: ").SemiBold(); t.Span($"{_data.Peak.PeakUpload:N0} B/s"); });
                    row.RelativeItem().AlignRight().Text(t => { t.Span("Lúc: ").SemiBold(); t.Span($"{_data.Peak.Time:HH:mm:ss}"); });
                });

                column.Item().PaddingBottom(5).Text("3. Dữ liệu chi tiết từng giây").FontSize(12).SemiBold().Underline();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        // --- THAY ĐỔI: Tăng PaddingVertical cho Header bảng ---
                        header.Cell().Element(CellStyle).PaddingVertical(8).Text("Thời gian").SemiBold();
                        header.Cell().Element(CellStyle).PaddingVertical(8).Text("Download (B/s)").SemiBold();
                        header.Cell().Element(CellStyle).PaddingVertical(8).Text("Upload (B/s)").SemiBold();
                    });

                    foreach (var point in _data.Points)
                    {
                        // --- THAY ĐỔI: Tăng PaddingVertical từ 5 lên 8 để các dòng trong bảng thưa ra ---
                        table.Cell().Element(CellStyle).PaddingVertical(8).Text(point.Time.ToString("HH:mm:ss"));
                        table.Cell().Element(CellStyle).PaddingVertical(8).Text(point.Download.ToString("N0"));
                        table.Cell().Element(CellStyle).PaddingVertical(8).Text(point.Upload.ToString("N0"));
                    }
                });
            });
        }

        static IContainer CardStyle(IContainer container) => 
            container.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(10);

        static IContainer CellStyle(IContainer container) => 
            container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).AlignCenter();
    }

    public static class StyleExtensions
    {
        public static TextSpanDescriptor HeaderStyle(this TextSpanDescriptor descriptor) 
            => descriptor.FontColor(Colors.Grey.Medium).SemiBold();
    }
}