using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure; // cần LicenseType + IContainer (QuestPDF)
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using QColors = QuestPDF.Helpers.Colors; // alias để không đụng System.Windows.Media.Colors

namespace MonitorApp.Views.Pages
{
    public partial class SpeedTest : UserControl, INotifyPropertyChanged
    {
        private DispatcherTimer _timer;
        private DateTime _startTime;

        // 0 = chạy giả (loading), 1 = reset về 0, 2 = đo thật
        private int _phase = 0;

        public double MaxSpeed { get; } = 999.9;

        private readonly Random _rng = new Random();
        private double _targetSpeed = 0;

        // =========================
        // ✅ METRICS (đồng bộ 3 card)
        // =========================
        private double _download;
        public string DownloadValue => _download.ToString("0.0");

        private double _upload;
        public string UploadValue => _upload.ToString("0.0");

        private int _ping;
        public string PingValue => _ping.ToString();

        private void SetMetrics(double download, double upload, int ping)
        {
            _download = download;
            _upload = upload;
            _ping = ping;

            OnPropertyChanged(nameof(DownloadValue));
            OnPropertyChanged(nameof(UploadValue));
            OnPropertyChanged(nameof(PingValue));
        }

        // =========================
        // ✅ LỊCH SỬ ĐO
        // =========================
        public ObservableCollection<HistoryRow> History { get; } = new ObservableCollection<HistoryRow>();

        public class HistoryRow
        {
            public string Date { get; set; } = "";
            public string Download { get; set; } = "";
            public string Upload { get; set; } = "";
            public string Ping { get; set; } = "";
        }

        // =========================
        // SPEED VALUE + ARC
        // =========================
        private double _speed;
        public double SpeedNumber
        {
            get => _speed;
            set
            {
                if (Math.Abs(_speed - value) < 0.0001) return;
                _speed = value;
                OnPropertyChanged(nameof(SpeedNumber));
                OnPropertyChanged(nameof(SpeedValue));

                UpdateArc(_speed / MaxSpeed);
            }
        }

        public string SpeedValue => SpeedNumber.ToString("000.0");

        public SpeedTest()
        {
            InitializeComponent();
            DataContext = this;

            SpeedNumber = 0;
            UpdateArc(0);

            SetMetrics(0, 0, 0);
            SetUiState(isTesting: false, finished: false);
        }

        // =========================
        // UI STATE
        // =========================
        private void SetUiState(bool isTesting, bool finished)
        {
            if (BtnStartTest != null)
                BtnStartTest.Visibility = (isTesting || finished) ? Visibility.Collapsed : Visibility.Visible;

            if (BtnRestart != null)
                BtnRestart.Visibility = finished ? Visibility.Visible : Visibility.Collapsed;
        }

        private void StartTest_Click(object sender, RoutedEventArgs e)
        {
            SetUiState(isTesting: true, finished: false);

            _phase = 0;
            _targetSpeed = 0;

            SpeedNumber = 0;
            UpdateArc(0);

            // reset card khi bắt đầu chạy
            SetMetrics(0, 0, 0);

            _startTime = DateTime.Now;

            _timer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _timer.Tick -= Timer_Tick;
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void ResetTest_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();

            _phase = 0;
            _targetSpeed = 0;

            SpeedNumber = 0;
            UpdateArc(0);

            SetMetrics(0, 0, 0);
            SetUiState(isTesting: false, finished: false);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var elapsed = (DateTime.Now - _startTime).TotalMilliseconds;
            var progress = Math.Min(elapsed / 1000.0, 1.0);

            if (_phase == 0)
            {
                // PHA 0: chạy giả 0 -> MaxSpeed
                SpeedNumber = progress * MaxSpeed;

                if (progress >= 1.0)
                {
                    _phase = 1;
                    SpeedNumber = 0;
                    _startTime = DateTime.Now;
                }
            }
            else if (_phase == 1)
            {
                if (elapsed >= 100)
                {
                    _phase = 2;
                    _startTime = DateTime.Now;

                    // demo kết quả đo thật (sau thay bằng đo thật)
                    _targetSpeed = _rng.NextDouble() * (350 - 50) + 50; // 50 -> 350
                    _targetSpeed = Math.Max(0, Math.Min(MaxSpeed, _targetSpeed));
                }
            }
            else if (_phase == 2)
            {
                // PHA 2: chạy 0 -> targetSpeed
                SpeedNumber = progress * _targetSpeed;

                if (progress >= 1.0)
                {
                    SpeedNumber = _targetSpeed;
                    _timer.Stop();

                    // demo metrics (sau thay bằng đo thật)
                    double download = _targetSpeed;
                    double upload = download * 0.40;
                    int ping = _rng.Next(5, 40);

                    // ✅ đồng bộ card
                    SetMetrics(download, upload, ping);

                    // ✅ ghi lịch sử
                    History.Insert(0, new HistoryRow
                    {
                        Date = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                        Download = $"{download:0.0} Mbps",
                        Upload = $"{upload:0.0} Mbps",
                        Ping = $"{ping} ms"
                    });

                    SetUiState(isTesting: false, finished: true);
                }
            }
        }

        // =========================
        // ✅ EXPORT PDF (nút Download)
        // =========================
        private void ExportHistoryPdf_Click(object sender, RoutedEventArgs e)
        {
            if (History == null || History.Count == 0)
            {
                MessageBox.Show("Chưa có dữ liệu Test History để xuất PDF.", "Export PDF",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "PDF file (*.pdf)|*.pdf",
                FileName = $"SpeedTest_History_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };

            if (dlg.ShowDialog() != true) return;

            QuestPDF.Settings.License = LicenseType.Community;

            var data = History.ToList();

            Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("SPEED TEST - TEST HISTORY").FontSize(18).SemiBold();
                        col.Item().Text($"Export time: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                                  .FontColor(QColors.Grey.Darken1);
                        col.Item().PaddingTop(8).LineHorizontal(1);
                    });

                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4); // Date
                            cols.RelativeColumn(3); // Download
                            cols.RelativeColumn(3); // Upload
                            cols.RelativeColumn(2); // Ping
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellHeader).Text("Date");
                            header.Cell().Element(CellHeader).Text("Download");
                            header.Cell().Element(CellHeader).Text("Upload");
                            header.Cell().Element(CellHeader).Text("Ping");
                        });

                        foreach (var row in data)
                        {
                            table.Cell().Element(CellBody).Text(row.Date);
                            table.Cell().Element(CellBody).Text(row.Download);
                            table.Cell().Element(CellBody).Text(row.Upload);
                            table.Cell().Element(CellBody).Text(row.Ping);
                        }
                    });

                    page.Footer().AlignRight().Text(t =>
                    {
                        t.Span("Page ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
            }).GeneratePdf(dlg.FileName);

            MessageBox.Show("Xuất PDF thành công!", "Export PDF",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ✅ FIX method-group + ambiguous IContainer: dùng FULLY QUALIFIED
        private static QuestPDF.Infrastructure.IContainer CellHeader(QuestPDF.Infrastructure.IContainer c) =>
            c.PaddingVertical(6).PaddingHorizontal(8)
             .Background(QColors.Grey.Lighten3)
             .Border(1).BorderColor(QColors.Grey.Lighten1)
             .DefaultTextStyle(x => x.SemiBold());

        private static QuestPDF.Infrastructure.IContainer CellBody(QuestPDF.Infrastructure.IContainer c) =>
            c.PaddingVertical(6).PaddingHorizontal(8)
             .Border(1).BorderColor(QColors.Grey.Lighten2);

        // (tuỳ chọn) nếu XAML còn gọi SelectionChanged thì giữ hàm trống
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // =========================
        // ARC DRAW
        // =========================
        private void UpdateArc(double progress01)
        {
            progress01 = Math.Max(0, Math.Min(1, progress01));

            const double cx = 210.0;
            const double cy = 210.0;
            const double r = 137.5;
            const double startAngle = -60.0;

            if (progress01 <= 0.0001)
            {
                ArcPath.Data = Geometry.Empty;
                return;
            }

            double sweep = 360.0 * progress01;
            double endAngle = startAngle + sweep;

            Point start = PointOnCircle(cx, cy, r, startAngle);
            Point end = PointOnCircle(cx, cy, r, endAngle);

            bool isLargeArc = sweep > 180.0;

            var fig = new PathFigure { StartPoint = start, IsClosed = false };
            fig.Segments.Add(new ArcSegment
            {
                Point = end,
                // ✅ FIX Size ambiguous: chỉ rõ System.Windows.Size
                Size = new System.Windows.Size(r, r),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise
            });

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            ArcPath.Data = geo;
        }

        private static Point PointOnCircle(double cx, double cy, double r, double angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            return new Point(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void DataGrid_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DataGrid_SelectionChanged_2(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DataGrid_SelectionChanged_3(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
