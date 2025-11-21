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

            _ = LoadDataAsync(); // Load lần đầu
        }

        private async Task LoadDataAsync()
        {
            var range = ParentViewModel?.SelectedRange ?? "5m";
            var data = await _trafficService.GetTrafficSummaryAsync(range);

            if (data != null)
            {
                TotalUsage = data.TotalUsage;
                DownloadTotal = data.DownloadTotal;
                DownloadSpeed = data.DownloadSpeed;
                UploadTotal = data.UploadTotal;
                UploadSpeed = data.UploadSpeed;
                WanUsage = data.WanUsage;
                LanUsage = data.LanUsage;
                DownloadRatio = data.DownloadRatio;
                UploadRatio = data.UploadRatio;
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