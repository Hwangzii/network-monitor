// ViewModels/PageViewModels/TrafficMonitor/TM_FooterViewModel.cs
using MonitorApp.Helpers;
using MonitorApp.Models;
using MonitorApp.Services.Traffic;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace MonitorApp.ViewModels.PageViewModels.TrafficMonitor
{
    public class TM_FooterViewModel : INotifyPropertyChanged
    {
        private readonly TrafficApiService _trafficService;
        private readonly DispatcherTimer _timer;

        public TrafficMonitorViewModel? ParentViewModel { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        // Properties
        private string _totalUsage = "0 MB"; public string TotalUsage { get => _totalUsage; set { _totalUsage = value; OnPropertyChanged(); } }
        private string _downloadTotal = "0 MB"; public string DownloadTotal { get => _downloadTotal; set { _downloadTotal = value; OnPropertyChanged(); } }
        private string _downloadSpeed = "0 B/s"; public string DownloadSpeed { get => _downloadSpeed; set { _downloadSpeed = value; OnPropertyChanged(); } }
        private string _uploadTotal = "0 MB"; public string UploadTotal { get => _uploadTotal; set { _uploadTotal = value; OnPropertyChanged(); } }
        private string _uploadSpeed = "0 B/s"; public string UploadSpeed { get => _uploadSpeed; set { _uploadSpeed = value; OnPropertyChanged(); } }
        private string _wanUsage = "0 MB"; public string WanUsage { get => _wanUsage; set { _wanUsage = value; OnPropertyChanged(); UpdateBars(); } }
        private string _lanUsage = "0 MB"; public string LanUsage { get => _lanUsage; set { _lanUsage = value; OnPropertyChanged(); UpdateBars(); } }
        private double _downloadRatio = 0; public double DownloadRatio { get => _downloadRatio; set { _downloadRatio = value; OnPropertyChanged(); } }
        private double _uploadRatio = 0; public double UploadRatio { get => _uploadRatio; set { _uploadRatio = value; OnPropertyChanged(); } }

        public double WanFillWidth { get; private set; } = 200;
        public double LanFillWidth { get; private set; } = 200;

        public ICommand RefreshCommand { get; }

        public TM_FooterViewModel()
        {
            _trafficService = new TrafficApiService();

            RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += async (s, e) => await LoadDataAsync();
            _timer.Start();

            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += OnTimerTick;
            timer.Start();
        }

        private async Task LoadDataAsync()
        {
            var range = ParentViewModel?.SelectedRange ?? "5m";
            var data = await _trafficService.GetTrafficSummaryAsync(range);

            if (data != null)
            {
                var t = lastLabelTime.AddSeconds(i * LabelIntervalSeconds);
                allTimeLabels.Add(t.ToString("h:mm:ss tt"));
            }

            lastLabelTime = lastLabelTime.AddSeconds(24 * LabelIntervalSeconds);

            UpdateVisibleTimeLabels();
        }

        private DateTime lastSpeedUpdateTime = DateTime.Now;
        private const double SpeedUpdateIntervalSeconds = 1.0; // 1s đổi speed 1 lần

        private void OnTimerTick(object sender, EventArgs e)
        {
            var now = DateTime.Now;

            // 1) Cập nhật speed mỗi 1 giây cho đỡ nhấp nháy
            if ((now - lastSpeedUpdateTime).TotalSeconds >= SpeedUpdateIntervalSeconds)
            {
                DownloadSpeed = rnd.Next(500_000, 50_000_000);   // ~0.5MB/s → 50MB/s
                UploadSpeed = rnd.Next(200_000, 20_000_000);     // ~0.2MB/s → 20MB/s

                lastSpeedUpdateTime = now;
            }

            // 2) Cập nhật total theo thời gian thực (mỗi frame 16ms)
            double elapsedTotal = (now - lastTotalTime).TotalSeconds;
            if (elapsedTotal > 0)
            {
                double dDown = DownloadSpeed * elapsedTotal;
                double dUp = UploadSpeed * elapsedTotal;

                // Chia increment cho WAN / LAN
                double wanDownShare = rnd.NextDouble(); // 0..1
                double wanUpShare = rnd.NextDouble();

                double wanDownInc = dDown * wanDownShare;
                double lanDownInc = dDown - wanDownInc;

                double wanUpInc = dUp * wanUpShare;
                double lanUpInc = dUp - wanUpInc;

                // Cộng vào totals interface
                wanDownloadTotal += wanDownInc;
                lanDownloadTotal += lanDownInc;
                wanUploadTotal += wanUpInc;
                lanUploadTotal += lanUpInc;

                // Tổng global = WAN + LAN
                DownloadTotal = wanDownloadTotal + lanDownloadTotal;
                UploadTotal = wanUploadTotal + lanUploadTotal;

                lastTotalTime = now;
            }

            // 3) TIMELINE – giống GraphViewModel
            double elapsedLabel = (now - lastLabelTime).TotalSeconds;
            var progress = elapsedLabel / LabelIntervalSeconds;
            SmoothScrollOffset = -(progress * LabelWidth);

            if (elapsedLabel >= LabelIntervalSeconds)
            {
                // Dời anchor theo đúng bước 4s (tránh drift)
                lastLabelTime = lastLabelTime.AddSeconds(LabelIntervalSeconds);

                allTimeLabels.Add(lastLabelTime.ToString("h:mm:ss tt"));
                if (allTimeLabels.Count > 200)
                    allTimeLabels.RemoveAt(0);

                SmoothScrollOffset = 0;
                UpdateVisibleTimeLabels();
            }
        }

        private void UpdateBars()
        {
            WanFillWidth = 200;
            LanFillWidth = 200;
            OnPropertyChanged(nameof(WanFillWidth));
            OnPropertyChanged(nameof(LanFillWidth));
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}