using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using MonitorApp.Services;
using NetworkMonitor.Core.Models;

namespace MonitorApp.ViewModels.Stores
{
    public class NetworkMonitorStore : INotifyPropertyChanged, IDisposable
    {
        // Singleton tiện dùng khắp nơi
        public static NetworkMonitorStore Instance { get; } =
            new NetworkMonitorStore(new MonitorApiClient());

        private readonly MonitorApiClient _api;
        private readonly DispatcherTimer _timer;

        private long _lastBytesSent;
        private long _lastBytesReceived;
        private DateTime _lastTime;

        private string _ip = "Unknown";
        private double _downloadKBps;
        private double _uploadKBps;
        private bool _isMonitoring;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Event bắn ra số byte tăng thêm mỗi lần status mới.
        /// downBytes = bytesReceived tăng, upBytes = bytesSent tăng.
        /// </summary>
        public event Action<double, double>? TrafficDeltaCalculated;

        public string Ip
        {
            get => _ip;
            private set { _ip = value; OnPropertyChanged(); }
        }

        public double DownloadKBps
        {
            get => _downloadKBps;
            private set { _downloadKBps = value; OnPropertyChanged(); }
        }

        public double UploadKBps
        {
            get => _uploadKBps;
            private set { _uploadKBps = value; OnPropertyChanged(); }
        }

        public bool IsMonitoring
        {
            get => _isMonitoring;
            private set { _isMonitoring = value; OnPropertyChanged(); }
        }

        private NetworkMonitorStore(MonitorApiClient api)
        {
            _api = api;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)   // 1s gọi API 1 lần
            };
            _timer.Tick += async (_, __) => await RefreshAsync();
        }

        // Có thể gọi từ UI (header) khi user bấm Start
        public async Task StartMonitoringAsync()
        {
            await _api.StartAsync();
            _lastTime = DateTime.MinValue;
            _timer.Start();
        }

        public async Task StopMonitoringAsync()
        {
            _timer.Stop();
            await _api.StopAsync();
            DownloadKBps = 0;
            UploadKBps = 0;
        }

        private async Task RefreshAsync()
        {
            MonitorStatus? status = null;

            try
            {
                status = await _api.GetStatusAsync();
            }
            catch
            {
                // TODO: log / hiển thị lỗi nếu cần
            }

            if (status == null)
                return;

            Ip = status.Ip;
            IsMonitoring = status.IsMonitoring;

            var now = DateTime.Now;

            if (_lastTime != DateTime.MinValue)
            {
                var elapsed = (now - _lastTime).TotalSeconds;
                if (elapsed > 0)
                {
                    var deltaSent = status.BytesSent - _lastBytesSent;       // up
                    var deltaRecv = status.BytesReceived - _lastBytesReceived; // down

                    // Byte/s -> KB/s
                    DownloadKBps = deltaRecv / 1024.0 / elapsed;
                    UploadKBps = deltaSent / 1024.0 / elapsed;

                    // Báo cho ViewModel khác biết có thêm bao nhiêu byte
                    TrafficDeltaCalculated?.Invoke(deltaRecv, deltaSent);
                }
            }

            _lastTime = now;
            _lastBytesSent = status.BytesSent;
            _lastBytesReceived = status.BytesReceived;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= async (_, __) => await RefreshAsync();
        }
    }
}
