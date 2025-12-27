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

        public SpeedTest()
        {
            InitializeComponent();
            DataContext = this;

            SpeedNumber = 0;
            SetMetrics(0, 0, 0);
            SetUiState(isTesting: false, finished: false);

            Loaded += async (_, __) => await LoadHistoryAsync(10);
            Unloaded += (_, __) => CancelRun();
        }

        private void SetUiState(bool isTesting, bool finished)
        {
            IsRunning = isTesting;

            if (BtnStartTest != null)
                BtnStartTest.Visibility = (isTesting || finished) ? Visibility.Collapsed : Visibility.Visible;

            if (BtnRestart != null)
                BtnRestart.Visibility = finished ? Visibility.Visible : Visibility.Collapsed;
        }


        private async void StartTest_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning) return;

            SetUiState(isTesting: true, finished: false);

            // ===== Reset UI =====
            SpeedNumber = 0;
            SetMetrics(0, 0, 0);

            StatusText = "Starting...";
            QualityText = "Good";

            // ===== bật vòng chạy =====
            if (RingProgress != null)
                RingProgress.Visibility = Visibility.Visible;

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

                            // có dữ liệu thật -> tắt vòng chạy
                            var t = (ev.Type ?? "").ToLowerInvariant();
                            if (t is "ping" or "download" or "upload" or "complete")
                            {
                                if (RingProgress != null)
                                    RingProgress.Visibility = Visibility.Collapsed;
                            }
                        });

                        if (string.Equals(ev.Type, "complete", StringComparison.OrdinalIgnoreCase))
                            break;
                    }
                }, ct);

                StatusText = "Completed";
                SetUiState(isTesting: false, finished: true);

                await LoadHistoryAsync(10);
            }
            catch (OperationCanceledException)
            {
                StatusText = "Canceled";
                SetUiState(isTesting: false, finished: false);
            }
            catch (HttpRequestException ex)
            {
                StatusText = "Network error";
                System.Diagnostics.Debug.WriteLine(ex);
                SetUiState(isTesting: false, finished: false);
            }
            catch (Exception ex)
            {
                StatusText = "Error";
                System.Diagnostics.Debug.WriteLine(ex);
                SetUiState(isTesting: false, finished: false);
                System.Diagnostics.Debug.WriteLine("SpeedTest ERROR: " + ex);
                MessageBox.Show(ex.Message, "SpeedTest Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // đảm bảo luôn tắt vòng chạy
                if (RingProgress != null)
                    RingProgress.Visibility = Visibility.Collapsed;
            }
        }

        private void ResetTest_Click(object sender, RoutedEventArgs e)
        {
            CancelRun();

            SpeedNumber = 0;
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
        // Load History from backend
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

        private static QuestPDF.Infrastructure.IContainer CellHeader(QuestPDF.Infrastructure.IContainer c) =>
            c.PaddingVertical(6).PaddingHorizontal(8)
             .Background(QColors.Grey.Lighten3)
             .Border(1).BorderColor(QColors.Grey.Lighten1)
             .DefaultTextStyle(x => x.SemiBold());

        private static QuestPDF.Infrastructure.IContainer CellBody(QuestPDF.Infrastructure.IContainer c) =>
            c.PaddingVertical(6).PaddingHorizontal(8)
             .Border(1).BorderColor(QColors.Grey.Lighten2);


        // =========================
        // DataGrid selection handlers (giữ trống nếu XAML gọi)
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
