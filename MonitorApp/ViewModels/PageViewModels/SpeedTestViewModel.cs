using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MonitorApp.Models;
using MonitorApp.Services;
using MonitorApp.ViewModels;

namespace MonitorApp.ViewModels.PageViewModels
{
    public class SpeedTestViewModel : BaseViewModel
    {
        private readonly MonitorApiClient _api = new();
        private CancellationTokenSource? _runCts;

        // ======= TOP LEFT (IP/Provider/Location) =======
        private string _ipAddress = "Not available";
        public string IpAddress
        {
            get => _ipAddress;
            set { if (_ipAddress != value) { _ipAddress = value; OnPropertyChanged(); } }
        }

        private string _provider = "Not available";
        public string Provider
        {
            get => _provider;
            set { if (_provider != value) { _provider = value; OnPropertyChanged(); } }
        }

        private string _location = "Not available";
        public string Location
        {
            get => _location;
            set { if (_location != value) { _location = value; OnPropertyChanged(); } }
        }

        // ======= CENTER / METRICS =======
        private string _speedValue = "0.00";
        public string SpeedValue
        {
            get => _speedValue;
            set { if (_speedValue != value) { _speedValue = value; OnPropertyChanged(); } }
        }

        private string _downloadValue = "0.00";
        public string DownloadValue
        {
            get => _downloadValue;
            set { if (_downloadValue != value) { _downloadValue = value; OnPropertyChanged(); } }
        }

        private string _uploadValue = "0.00";
        public string UploadValue
        {
            get => _uploadValue;
            set { if (_uploadValue != value) { _uploadValue = value; OnPropertyChanged(); } }
        }

        private string _pingValue = "0.00";
        public string PingValue
        {
            get => _pingValue;
            set { if (_pingValue != value) { _pingValue = value; OnPropertyChanged(); } }
        }

        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set { if (_statusText != value) { _statusText = value; OnPropertyChanged(); } }
        }

        private string _qualityText = "Good";
        public string QualityText
        {
            get => _qualityText;
            set { if (_qualityText != value) { _qualityText = value; OnPropertyChanged(); } }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set { if (_isRunning != value) { _isRunning = value; OnPropertyChanged(); } }
        }

        // ======= HISTORY =======
        public ObservableCollection<SpeedHistoryRow> History { get; } = new();

        public async Task InitializeAsync() => await LoadHistoryAsync(10);

        public async Task LoadHistoryAsync(int limit = 10)
        {
            try
            {
                if (limit <= 0) limit = 10;

                var resp = await _api.GetSpeedHistoryAsync(limit);
                History.Clear();

                if (resp?.Success == true && resp.Data != null)
                {
                    foreach (var item in resp.Data.OrderByDescending(x => x.Timestamp))
                    {
                        History.Add(new SpeedHistoryRow
                        {
                            Id = item.Id,
                            Date = item.Timestamp.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                            Download = item.DownloadMbps,
                            Upload = item.UploadMbps,
                            Ping = item.PingMs
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadHistoryAsync error: " + ex);
            }
        }

        public async Task StartTestAsync()
        {
            if (IsRunning) return;

            ResetValues(keepHistory: true);

            IsRunning = true;
            StatusText = "Starting...";
            QualityText = "Good";

            _runCts?.Cancel();
            _runCts?.Dispose();
            _runCts = new CancellationTokenSource();
            var ct = _runCts.Token;

            try
            {
                await foreach (var ev in _api.SpeedRunStreamAsync(ct))
                {
                    // UI thread safe
                    Application.Current.Dispatcher.Invoke(() => ApplyEvent(ev));

                    if (string.Equals(ev?.Type, "complete", StringComparison.OrdinalIgnoreCase))
                        break;
                }

                StatusText = "Completed";
                await LoadHistoryAsync(10);
            }
            catch (OperationCanceledException)
            {
                StatusText = "Canceled";
            }
            catch (Exception ex)
            {
                StatusText = "Error";
                System.Diagnostics.Debug.WriteLine("SpeedTest error: " + ex);
            }
            finally
            {
                IsRunning = false;
            }
        }

        public void Cancel()
        {
            try { _runCts?.Cancel(); } catch { }
        }

        public void ResetValues(bool keepHistory = true)
        {
            Cancel();

            SpeedValue = "0.00";
            DownloadValue = "0.00";
            UploadValue = "0.00";
            PingValue = "0.00";
            StatusText = "Ready";
            QualityText = "Good";

            // nếu muốn reset info về "Not available" khi restart thì mở 3 dòng này:
            // IpAddress = "Not available";
            // Provider  = "Not available";
            // Location  = "Not available";

            if (!keepHistory) History.Clear();
        }

        private void ApplyEvent(SpeedEvent ev)
        {
            if (ev?.Data == null) return;

            var type = (ev.Type ?? "").Trim().ToLowerInvariant();

            if (type == "status")
            {
                // ưu tiên message nếu backend có
                StatusText = ev.Data.Message ?? ev.Data.Quality ?? "Running...";
                return;
            }

            if (type == "ping")
            {
                var p = ev.Data.PingMs;
                if (p.HasValue)
                {
                    PingValue = p.Value.ToString("0.00", CultureInfo.InvariantCulture);
                    SpeedValue = PingValue;
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
                    DownloadValue = d.Value.ToString("0.00", CultureInfo.InvariantCulture);
                    SpeedValue = DownloadValue;
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
                    UploadValue = u.Value.ToString("0.00", CultureInfo.InvariantCulture);
                    SpeedValue = UploadValue;
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

                if (d.HasValue) DownloadValue = d.Value.ToString("0.00", CultureInfo.InvariantCulture);
                if (u.HasValue) UploadValue = u.Value.ToString("0.00", CultureInfo.InvariantCulture);
                if (p.HasValue) PingValue = p.Value.ToString("0.00", CultureInfo.InvariantCulture);

                if (!string.IsNullOrWhiteSpace(ev.Data.IpAddress)) IpAddress = ev.Data.IpAddress!;
                if (!string.IsNullOrWhiteSpace(ev.Data.Provider)) Provider = ev.Data.Provider!;
                if (!string.IsNullOrWhiteSpace(ev.Data.Location)) Location = ev.Data.Location!;

                SpeedValue = DownloadValue;
                StatusText = "Test completed";
            }
        }
    }
}
