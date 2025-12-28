using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using QColors = QuestPDF.Helpers.Colors;

using MonitorApp.Models;
using MonitorApp.Services;

namespace MonitorApp.Views.Pages
{
    public partial class SpeedTest : UserControl, INotifyPropertyChanged
    {
        // =========================
        // API
        // =========================
        private readonly MonitorApiClient _api = new();
        private CancellationTokenSource? _runCts;

        public double MaxSpeed { get; } = 999.9;

        // =========================
        // OPTIONAL: Info (nếu XAML bind)
        // =========================
        private string _ipAddress = "Not available";
        public string IpAddress { get => _ipAddress; set { _ipAddress = value; OnPropertyChanged(); } }

        private string _provider = "Not available";
        public string Provider { get => _provider; set { _provider = value; OnPropertyChanged(); } }

        private string _location = "Not available";
        public string Location { get => _location; set { _location = value; OnPropertyChanged(); } }

        private string _statusText = "Ready";
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }

        private string _qualityText = "Good";
        public string QualityText { get => _qualityText; set { _qualityText = value; OnPropertyChanged(); } }

        // =========================
        // METRICS (3 card)
        // =========================
        private double _download;
        public string DownloadValue => _download.ToString("0.0");

        private double _upload;
        public string UploadValue => _upload.ToString("0.0");

        private double _ping;
        public string PingValue => _ping.ToString("0.0");

        private void SetMetrics(double? download, double? upload, double? ping)
        {
            if (download.HasValue) _download = download.Value;
            if (upload.HasValue) _upload = upload.Value;
            if (ping.HasValue) _ping = ping.Value;

            OnPropertyChanged(nameof(DownloadValue));
            OnPropertyChanged(nameof(UploadValue));
            OnPropertyChanged(nameof(PingValue));
        }

        // =========================
        // HISTORY (DataGrid)
        // =========================
        public ObservableCollection<HistoryRow> History { get; } = new ObservableCollection<HistoryRow>();

        public class HistoryRow
        {
            public string Date { get; set; } = "";
            public double Download { get; set; }
            public double Upload { get; set; }
            public double Ping { get; set; }
        }

        // =========================
        // SPEED VALUE
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
            }
        }

        public string SpeedValue => SpeedNumber.ToString("000.0");

        // =========================
        // UI STATE
        // =========================
        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning == value) return;
                _isRunning = value;
                OnPropertyChanged();
            }
        }

        private bool _isFinished;
        public bool IsFinished
        {
            get => _isFinished;
            set
            {
                if (_isFinished == value) return;
                _isFinished = value;
                OnPropertyChanged();
            }
        }

        // =========================
        // RING SWEEP (0->360 mượt)
        // =========================
        private readonly DispatcherTimer _ringTimer = new DispatcherTimer();
        private DateTime _ringStart;
        private const double RingCycleSeconds = 1.2; // chậm hơn => tăng số này

        private double _ringProgress01; // 0..1
        public double RingProgress01
        {
            get => _ringProgress01;
            set
            {
                value = Math.Max(0, Math.Min(1, value));
                if (Math.Abs(_ringProgress01 - value) < 0.000001) return;
                _ringProgress01 = value;
                DrawRingArc(_ringProgress01);
            }
        }

        private void StartRing()
        {
            RingProgress01 = 0;

            if (RingSweep != null) RingSweep.Visibility = Visibility.Visible;
            if (RingHead != null) RingHead.Visibility = Visibility.Visible;

            _ringStart = DateTime.Now;
            _ringTimer.Start();
        }

        private void StopRing(bool showFull)
        {
            _ringTimer.Stop();

            if (showFull)
            {
                RingProgress01 = 1.0;
                if (RingSweep != null) RingSweep.Visibility = Visibility.Visible;

                // khi full thì head ẩn (hoặc để Visible nếu bạn thích)
                if (RingHead != null) RingHead.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (RingSweep != null) RingSweep.Visibility = Visibility.Collapsed;
                if (RingHead != null) RingHead.Visibility = Visibility.Collapsed;
            }
        }


        private void DrawRingArc(double p01)
        {
            if (RingSweep == null) return;

            const double size = 275.0;
            const double stroke = 16.0;
            double r = (size - stroke) / 2.0;

            double cx = size / 2.0;
            double cy = size / 2.0;

            const double startAngle = -90.0; // 12h
            double sweep = 360.0 * p01;

            // --- fill arc ---
            if (sweep <= 0.001)
            {
                RingSweep.Data = Geometry.Empty;
                if (RingHead != null) RingHead.Data = Geometry.Empty;
                return;
            }

            double endAngle = startAngle + sweep;

            Point start = PointOnCircle(cx, cy, r, startAngle);
            Point end = PointOnCircle(cx, cy, r, endAngle);

            bool large = sweep > 180.0;

            var fig = new PathFigure { StartPoint = start, IsClosed = false };
            fig.Segments.Add(new ArcSegment
            {
                Point = end,
                Size = new System.Windows.Size(r, r),   // FIX ambiguous Size
                IsLargeArc = large,
                SweepDirection = SweepDirection.Clockwise
            });

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            RingSweep.Data = geo;

            // --- head arc (radar tip) ---
            if (RingHead != null)
            {
                // độ dài “đầu radar” (độ). tăng lên nếu muốn đầu dài hơn
                const double headSweepDeg = 36.0;

                // khi sweep nhỏ hơn headSweep, head sẽ bám theo đoạn đã có
                double headStart = Math.Max(startAngle, endAngle - headSweepDeg);
                double headEnd = endAngle;

                Point hs = PointOnCircle(cx, cy, r, headStart);
                Point he = PointOnCircle(cx, cy, r, headEnd);

                bool headLarge = (headEnd - headStart) > 180.0;

                var hFig = new PathFigure { StartPoint = hs, IsClosed = false };
                hFig.Segments.Add(new ArcSegment
                {
                    Point = he,
                    Size = new System.Windows.Size(r, r),
                    IsLargeArc = headLarge,
                    SweepDirection = SweepDirection.Clockwise
                });

                var hGeo = new PathGeometry();
                hGeo.Figures.Add(hFig);
                RingHead.Data = hGeo;
            }
        }

        private static Point PointOnCircle(double cx, double cy, double r, double angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            return new Point(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
        }

        // =========================
        // Fake speed while waiting
        // =========================
        private readonly DispatcherTimer _fakeSpeedTimer = new DispatcherTimer();
        private readonly Random _rng = new Random();
        private bool _hasRealData;

        private void StartFakeSpeed()
        {
            _hasRealData = false;
            _fakeSpeedTimer.Start();
        }

        private void StopFakeSpeed()
        {
            _hasRealData = true;
            _fakeSpeedTimer.Stop();
        }

        public SpeedTest()
        {
            InitializeComponent();
            DataContext = this;

            // Ring 60fps
            _ringTimer.Interval = TimeSpan.FromMilliseconds(16);
            _ringTimer.Tick += (_, __) =>
            {
                if (!IsRunning) return;

                var t = (DateTime.Now - _ringStart).TotalSeconds;
                var p = (t % RingCycleSeconds) / RingCycleSeconds;
                RingProgress01 = p;
            };

            // Fake speed ~16fps
            _fakeSpeedTimer.Interval = TimeSpan.FromMilliseconds(60);
            _fakeSpeedTimer.Tick += (_, __) =>
            {
                if (!IsRunning || _hasRealData) return;

                var baseMax = Math.Min(250.0, MaxSpeed);
                var v = 20.0 + _rng.NextDouble() * (baseMax - 20.0);
                SpeedNumber = v;
            };

            SpeedNumber = 0;
            SetMetrics(0, 0, 0);
            SetUiState(isTesting: false, finished: false);

            Loaded += async (_, __) => await LoadHistoryAsync(10);
            Unloaded += (_, __) => CancelRun();
        }

        private void SetUiState(bool isTesting, bool finished)
        {
            IsRunning = isTesting;
            IsFinished = finished;

            if (BtnStartTest != null)
                BtnStartTest.Visibility = (isTesting || finished) ? Visibility.Collapsed : Visibility.Visible;

            if (BtnRestart != null)
                BtnRestart.Visibility = finished ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void StartTest_Click(object sender, RoutedEventArgs e)
        {
            if (IsRunning) return;

            SetUiState(isTesting: true, finished: false);

            // Reset UI
            SpeedNumber = 0;
            SetMetrics(0, 0, 0);
            StatusText = "Starting...";
            QualityText = "Good";

            // Start visuals
            StartRing();
            StartFakeSpeed();

            CancelRun();
            _runCts = new CancellationTokenSource();
            var ct = _runCts.Token;

            try
            {
                await Task.Run(async () =>
                {
                    await foreach (var ev in _api.SpeedRunStreamAsync(ct))
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            ApplySpeedEvent_OnUI(ev);

                            var t = (ev.Type ?? "").ToLowerInvariant();
                            if (t is "ping" or "download" or "upload" or "complete")
                                StopFakeSpeed();
                        });

                        if (string.Equals(ev.Type, "complete", StringComparison.OrdinalIgnoreCase))
                            break;
                    }
                }, ct);

                StatusText = "Completed";
                SetUiState(isTesting: false, finished: true);

                // full vòng khi xong
                StopRing(showFull: true);

                await LoadHistoryAsync(10);
            }
            catch (OperationCanceledException)
            {
                StatusText = "Canceled";
                SetUiState(isTesting: false, finished: false);
                StopRing(showFull: false);
            }
            catch (HttpRequestException ex)
            {
                StatusText = "Network error";
                System.Diagnostics.Debug.WriteLine(ex);
                SetUiState(isTesting: false, finished: false);
                StopRing(showFull: false);
            }
            catch (Exception ex)
            {
                StatusText = "Error";
                System.Diagnostics.Debug.WriteLine(ex);
                SetUiState(isTesting: false, finished: false);
                StopRing(showFull: false);

                System.Diagnostics.Debug.WriteLine("SpeedTest ERROR: " + ex);
                MessageBox.Show(ex.Message, "SpeedTest Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _fakeSpeedTimer.Stop();
                _ringTimer.Stop();
            }
        }

        private void ResetTest_Click(object sender, RoutedEventArgs e)
        {
            CancelRun();

            _fakeSpeedTimer.Stop();
            _ringTimer.Stop();
            _hasRealData = false;

            SpeedNumber = 0;
            RingProgress01 = 0;
            if (RingSweep != null) RingSweep.Visibility = Visibility.Collapsed;

            SetMetrics(0, 0, 0);
            StatusText = "Ready";
            QualityText = "Good";

            SetUiState(isTesting: false, finished: false);
        }

        private void CancelRun()
        {
            try { _runCts?.Cancel(); } catch { }
            _runCts?.Dispose();
            _runCts = null;
        }

        // =========================
        // Apply stream events
        // =========================
        private void ApplySpeedEvent_OnUI(SpeedEvent ev)
        {
            if (ev?.Data == null) return;

            var type = (ev.Type ?? "").ToLowerInvariant();

            if (type == "status")
            {
                StatusText = ev.Data.Message ?? "Running...";
                return;
            }

            if (type == "ping")
            {
                var ping = ev.Data.PingMs;
                if (ping.HasValue)
                {
                    SetMetrics(null, null, ping.Value);
                    SpeedNumber = Math.Min(MaxSpeed, ping.Value);
                }

                if (!string.IsNullOrWhiteSpace(ev.Data.Quality))
                    QualityText = ev.Data.Quality!;

                StatusText = "Ping...";
                return;
            }

            if (type == "download")
            {
                var d = ev.Data.DownloadMbps;
                if (d.HasValue)
                {
                    SetMetrics(d.Value, null, null);
                    SpeedNumber = Math.Min(MaxSpeed, d.Value);
                }

                if (!string.IsNullOrWhiteSpace(ev.Data.Quality))
                    QualityText = ev.Data.Quality!;

                StatusText = "Download...";
                return;
            }

            if (type == "upload")
            {
                var u = ev.Data.UploadMbps;
                if (u.HasValue)
                {
                    SetMetrics(null, u.Value, null);
                    SpeedNumber = Math.Min(MaxSpeed, u.Value);
                }

                if (!string.IsNullOrWhiteSpace(ev.Data.Quality))
                    QualityText = ev.Data.Quality!;

                StatusText = "Upload...";
                return;
            }

            if (type == "complete")
            {
                SetUiState(isTesting: false, finished: true);

                var d = ev.Data.DownloadMbps;
                var u = ev.Data.UploadMbps;
                var p = ev.Data.PingMs;

                if (d.HasValue) _download = d.Value;
                if (u.HasValue) _upload = u.Value;
                if (p.HasValue) _ping = p.Value;

                OnPropertyChanged(nameof(DownloadValue));
                OnPropertyChanged(nameof(UploadValue));
                OnPropertyChanged(nameof(PingValue));

                if (!string.IsNullOrWhiteSpace(ev.Data.IpAddress)) IpAddress = ev.Data.IpAddress!;
                if (!string.IsNullOrWhiteSpace(ev.Data.Provider)) Provider = ev.Data.Provider!;
                if (!string.IsNullOrWhiteSpace(ev.Data.Location)) Location = ev.Data.Location!;

                SpeedNumber = Math.Min(MaxSpeed, _download);
                StatusText = "Test completed";
            }
        }

        // =========================
        // Load History
        // =========================
        private async Task LoadHistoryAsync(int limit)
        {
            try
            {
                var resp = await _api.GetSpeedHistoryAsync(limit);
                History.Clear();

                if (resp?.Success != true || resp.Data == null) return;

                foreach (var item in resp.Data.OrderByDescending(x => x.Timestamp))
                {
                    History.Add(new HistoryRow
                    {
                        Date = item.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"),
                        Download = item.DownloadMbps,
                        Upload = item.UploadMbps,
                        Ping = item.PingMs
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        // =========================
        // EXPORT PDF
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
                            cols.RelativeColumn(4);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
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


            // Hỏi mở file
            var result = MessageBox.Show(
                "File đã được lưu. Bạn có muốn mở không?",
                "Export PDF",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // mở bằng app mặc định của Windows
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dlg.FileName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể mở file PDF.\n" + ex.Message, "Export PDF",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private static QuestPDF.Infrastructure.IContainer CellHeader(QuestPDF.Infrastructure.IContainer c) =>
            c.PaddingVertical(6).PaddingHorizontal(8)
             .Background(QColors.Grey.Lighten3)
             .Border(1).BorderColor(QColors.Grey.Lighten1)
             .DefaultTextStyle(x => x.SemiBold());

        private static QuestPDF.Infrastructure.IContainer CellBody(QuestPDF.Infrastructure.IContainer c) =>
            c.PaddingVertical(6).PaddingHorizontal(8)
             .Border(1).BorderColor(QColors.Grey.Lighten2);

        // =========================
        // DataGrid selection handlers
        // =========================
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void DataGrid_SelectionChanged_1(object sender, SelectionChangedEventArgs e) { }
        private void DataGrid_SelectionChanged_2(object sender, SelectionChangedEventArgs e) { }
        private void DataGrid_SelectionChanged_3(object sender, SelectionChangedEventArgs e) { }

        // =========================
        // INotifyPropertyChanged
        // =========================
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
