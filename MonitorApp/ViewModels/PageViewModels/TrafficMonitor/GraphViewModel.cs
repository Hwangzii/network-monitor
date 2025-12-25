using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MonitorApp.Models;
using MonitorApp.Services;

namespace MonitorApp.ViewModels.PageViewModels.TrafficMonitor
{
    public class GraphViewModel : INotifyPropertyChanged
    {
        private readonly MonitorApiClient _api = new();

        private readonly DispatcherTimer _dataTimer;   // gọi API theo range
        private readonly DispatcherTimer _axisTimer;   // chạy trục thời gian mượt 60fps

        private double _viewportWidth = 800;
        public double ViewportWidth
        {
            get => _viewportWidth;
            private set { _viewportWidth = value; OnPropertyChanged(); }
        }

        private double _chartWidth = 800;
        public double ChartWidth
        {
            get => _chartWidth;
            private set { _chartWidth = value; OnPropertyChanged(); }
        }

        private double _dynamicMaxValue = 1;
        public double DynamicMaxValue
        {
            get => _dynamicMaxValue;
            private set { _dynamicMaxValue = value <= 0 ? 1 : value; OnPropertyChanged(); OnPropertyChanged(nameof(MaxLabel)); }
        }

        public string MaxLabel => FormatDataRate(DynamicMaxValue);

        public ObservableCollection<double> DownloadData { get; } = new();
        public ObservableCollection<double> UploadData { get; } = new();
        public ObservableCollection<double> XPoints { get; } = new();
        public ObservableCollection<TimeTick> TimeTicks { get; } = new();

        private double _downloadSpeed;
        public double DownloadSpeed
        {
            get => _downloadSpeed;
            private set { _downloadSpeed = value; OnPropertyChanged(); }
        }

        private double _uploadSpeed;
        public double UploadSpeed
        {
            get => _uploadSpeed;
            private set { _uploadSpeed = value; OnPropertyChanged(); }
        }

        private string _selectedRange = "5m";
        public string SelectedRange
        {
            get => _selectedRange;
            private set { _selectedRange = value; OnPropertyChanged(); }
        }

        private TrafficChartPoint[] _lastPoints = Array.Empty<TrafficChartPoint>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public GraphViewModel()
        {
            TrafficRangeBus.RangeChanged += r =>
            {
                Application.Current.Dispatcher.Invoke(() => ChangeRange(r));
            };

            SelectedRange = NormalizeRange(TrafficRangeBus.CurrentRange);

            // API refresh timer
            _dataTimer = new DispatcherTimer();
            _dataTimer.Tick += async (_, __) => await LoadChartAsync(SelectedRange);
            ApplyRefreshInterval(SelectedRange);

            // Axis smooth timer (60fps)
            _axisTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60fps
            };
            _axisTimer.Tick += (_, __) =>
            {
                // chỉ update trục + x theo NOW để mượt
                UpdateAxisAndX_Smooth();
            };
            _axisTimer.Start();

            _ = LoadChartAsync(SelectedRange);
        }

        public void SetViewportWidth(double width)
        {
            if (width <= 0) return;
            ViewportWidth = width;
            ChartWidth = width; // luôn full màn hình
            UpdateAxisAndX_Smooth();
        }

        public void ChangeRange(string range)
        {
            range = NormalizeRange(range);
            if (range == SelectedRange) return;

            SelectedRange = range;
            ApplyRefreshInterval(range);
            _ = LoadChartAsync(range);

            UpdateAxisAndX_Smooth();
        }

        private static string NormalizeRange(string range)
        {
            range = (range ?? "").Trim().ToLowerInvariant();
            return range switch
            {
                "5m" => "5m",
                "3h" => "3h",
                "24h" => "24h",
                "24 hours" => "24h",
                _ => "5m"
            };
        }

        private (TimeSpan range, TimeSpan tickStep) GetRangeConfig(string range)
        {
            return range switch
            {
                "5m" => (TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(20)),
                "3h" => (TimeSpan.FromHours(3), TimeSpan.FromMinutes(10)),
                "24h" => (TimeSpan.FromHours(24), TimeSpan.FromHours(1)),
                _ => (TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(20))
            };
        }

        private string GetTickFormat(string range)
        {
            // 5m có giây + AM/PM như mẫu
            if (range == "5m") return "h:mm:ss tt";
            return "h:mm tt";
        }

        private double PxPerSecond()
        {
            var (r, _) = GetRangeConfig(SelectedRange);
            double sec = Math.Max(1, r.TotalSeconds);
            return ViewportWidth / sec; // NOW luôn sát phải
        }

        private async Task LoadChartAsync(string range)
        {
            try
            {
                var resp = await _api.GetTrafficChartAsync(range);
                if (resp?.Points == null) return;

                _lastPoints = resp.Points.ToArray();

                if (resp.MaxY > 0)
                    DynamicMaxValue = resp.MaxY;
                else if (_lastPoints.Length > 0)
                {
                    var max = _lastPoints.Max(p => Math.Max(p.Download, p.Upload));
                    DynamicMaxValue = max * 1.2;
                }

                // cập nhật series theo NOW (sẽ dùng chung hàm smooth)
                UpdateAxisAndX_Smooth();
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// Update XPoints + TimeTicks mượt theo NOW (không clamp Left để tránh "kẹt")
        /// </summary>
        private void UpdateAxisAndX_Smooth()
        {
            if (ViewportWidth <= 0) return;

            var (range, tickStep) = GetRangeConfig(SelectedRange);
            var nowUtc = DateTime.UtcNow;
            var windowStartUtc = nowUtc - range;

            double pxPerSec = PxPerSecond();

            // ====== Series X (align theo windowStart -> NOW ở mép phải) ======
            DownloadData.Clear();
            UploadData.Clear();
            XPoints.Clear();

            var pts = _lastPoints
                .Where(p => p.Time.ToUniversalTime() >= windowStartUtc && p.Time.ToUniversalTime() <= nowUtc)
                .OrderBy(p => p.Time)
                .ToArray();

            foreach (var p in pts)
            {
                var t = p.Time.ToUniversalTime();
                double x = (t - windowStartUtc).TotalSeconds * pxPerSec;

                // clamp X trong viewport để khỏi vẽ vượt
                if (x < 0) x = 0;
                if (x > ViewportWidth) x = ViewportWidth;

                XPoints.Add(x);
                DownloadData.Add(p.Download);
                UploadData.Add(p.Upload);
            }

            if (pts.Length > 0)
            {
                DownloadSpeed = pts[^1].Download;
                UploadSpeed = pts[^1].Upload;
            }
            else
            {
                DownloadSpeed = 0;
                UploadSpeed = 0;
            }

            // ====== TimeTicks (mượt) ======
            BuildTimeTicksSmooth(windowStartUtc, nowUtc, tickStep, pxPerSec);

            OnPropertyChanged(nameof(DownloadData));
            OnPropertyChanged(nameof(UploadData));
            OnPropertyChanged(nameof(XPoints));
            OnPropertyChanged(nameof(TimeTicks));
        }

        /// <summary>
        /// Sinh tick dựa trên tick gần NOW nhất ở phía phải, rồi kéo sang trái.
        /// Tick/label sẽ trôi liên tục theo mili-giây => không khựng.
        /// </summary>
        private void BuildTimeTicksSmooth(DateTime windowStartUtc, DateTime nowUtc, TimeSpan tickStep, double pxPerSec)
        {
            TimeTicks.Clear();

            double labelWidth = SelectedRange == "5m" ? 95 : 70;
            string fmt = GetTickFormat(SelectedRange);

            long stepTicks = tickStep.Ticks;
            if (stepTicks <= 0) return;

            // tick "chuẩn" gần NOW nhất (làm mốc bên phải)
            long nowTicks = nowUtc.Ticks;
            long rightTickTicks = (nowTicks / stepTicks) * stepTicks;
            var rightTickUtc = new DateTime(rightTickTicks, DateTimeKind.Utc);

            // NOW lệch bao nhiêu so với rightTick => tick trôi liên tục
            double offsetSec = (nowUtc - rightTickUtc).TotalSeconds; // [0..step)
            double rightX = ViewportWidth - offsetSec * pxPerSec;    // tick bên phải trôi sang trái

            double stepPx = tickStep.TotalSeconds * pxPerSec;
            if (stepPx <= 0.1) stepPx = 0.1;

            // đi từ phải sang trái
            for (int i = 0; ; i++)
            {
                var t = rightTickUtc - TimeSpan.FromTicks(stepTicks * (long)i);
                double x = rightX - i * stepPx;

                // stop khi tick đã ra khỏi màn hình trái đủ xa
                if (x < -labelWidth) break;

                // bỏ tick nằm ngoài window start (tránh label vô nghĩa)
                if (t < windowStartUtc) break;

                // Left = x - labelWidth/2 (KHÔNG clamp để tránh bị "kẹt" ở mép)
                double left = x - labelWidth / 2.0;

                TimeTicks.Add(new TimeTick
                {
                    Left = left,
                    LabelWidth = labelWidth,
                    Label = t.ToLocalTime().ToString(fmt)
                });
            }

            // hiện tại đang add từ phải->trái, muốn thứ tự trái->phải thì reverse
            // (không bắt buộc nhưng dễ đọc/debug)
            if (TimeTicks.Count > 1)
            {
                var reversed = TimeTicks.Reverse().ToList();
                TimeTicks.Clear();
                foreach (var tt in reversed) TimeTicks.Add(tt);
            }
        }

        private void ApplyRefreshInterval(string range)
        {
            _dataTimer.Stop();
            _dataTimer.Interval = range switch
            {
                "5m" => TimeSpan.FromSeconds(2),
                "3h" => TimeSpan.FromSeconds(20),
                "24h" => TimeSpan.FromMinutes(1),
                _ => TimeSpan.FromSeconds(2)
            };
            _dataTimer.Start();
        }

        private static string FormatDataRate(double kb)
        {
            if (kb < 1024) return $"{kb:F1} KB/s";
            if (kb < 1024 * 1024) return $"{kb / 1024:F1} MB/s";
            return $"{kb / 1024 / 1024:F1} GB/s";
        }

        protected void OnPropertyChanged([CallerMemberName] string name = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
