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
        private readonly DispatcherTimer _axisTimer;   // chạy trục thời gian mượt
        private TrafficChartPoint[] _visiblePoints = Array.Empty<TrafficChartPoint>();
        // ✅ MaxY trục sao cho đỉnh cao nhất chiếm ~75% chiều cao chart
        private const double PeakFillRatio = 0.95;
        // ✅ mượt scale Y
        private const double SmoothAlpha = 0.18;

        private double _targetMaxValue = 1;

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

        // Crosshair state
        private bool _isHovering;
        public bool IsHovering
        {
            get => _isHovering;
            set { _isHovering = value; OnPropertyChanged(); }
        }

        private double _hoverX;            // X theo tọa độ ChartCanvas (đã trừ scroll offset để vẽ)
        public double HoverX
        {
            get => _hoverX;
            set { _hoverX = value; OnPropertyChanged(); }
        }

        private double _hoverDownloadY;    // Y theo tọa độ ChartCanvas
        public double HoverDownloadY
        {
            get => _hoverDownloadY;
            set { _hoverDownloadY = value; OnPropertyChanged(); }
        }

        private double _hoverUploadY;      // Y theo tọa độ ChartCanvas
        public double HoverUploadY
        {
            get => _hoverUploadY;
            set { _hoverUploadY = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// ✅ Đây là MaxY của trục Y đang dùng để scale (converter đang bind vào)
        /// </summary>
        private double _dynamicMaxValue = 1;
        public double DynamicMaxValue
        {
            get => _dynamicMaxValue;
            private set
            {
                _dynamicMaxValue = value <= 0 ? 1 : value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MaxLabel));
            }
        }

        /// <summary>
        /// ✅ Label hiển thị đúng MaxY của trục Y (thước đo scale)
        /// </summary>
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

            _dataTimer = new DispatcherTimer();
            _dataTimer.Tick += async (_, __) => await LoadChartAsync(SelectedRange);
            ApplyRefreshInterval(SelectedRange);

            _axisTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _axisTimer.Tick += (_, __) =>
            {
                UpdateAxisAndX_Smooth(); // tick + series + scale
            };
            _axisTimer.Start();

            _ = LoadChartAsync(SelectedRange);
        }

        public void SetViewportWidth(double width)
        {
            if (width <= 0) return;
            ViewportWidth = width;
            ChartWidth = width;
            UpdateAxisAndX_Smooth();
        }

        public void ChangeRange(string range)
        {
            range = NormalizeRange(range);
            if (range == SelectedRange) return;

            SelectedRange = range;
            ApplyRefreshInterval(range);
            _ = LoadChartAsync(range);

            // update ngay UI
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
            if (range == "5m") return "h:mm:ss tt";
            return "h:mm tt";
        }

        private double PxPerSecond()
        {
            var (r, _) = GetRangeConfig(SelectedRange);
            double sec = Math.Max(1, r.TotalSeconds);
            return ViewportWidth / sec;
        }

        private async Task LoadChartAsync(string range)
        {
            try
            {
                var resp = await _api.GetTrafficChartAsync(range);
                if (resp?.Points == null) return;

                _lastPoints = resp.Points.ToArray();

                // ✅ KHÔNG set DynamicMaxValue theo resp.MaxY nữa
                // ✅ scale sẽ tính theo window hiển thị trong UpdateAxisAndX_Smooth()

                UpdateAxisAndX_Smooth();
            }
            catch
            {
                // ignore
            }
        }

        // ===== Y-scale helpers =====
        private static double NiceCeil(double value)
        {
            if (value <= 0) return 1;

            double exp = Math.Floor(Math.Log10(value));
            double f = value / Math.Pow(10, exp);

            double niceF = f <= 1 ? 1
                       : f <= 2 ? 2
                       : f <= 5 ? 5
                       : 10;

            return niceF * Math.Pow(10, exp);
        }

        private void UpdateYScaleTarget(double maxVisible)
        {
            if (maxVisible <= 0) maxVisible = 1;

            // ✅ maxVisible chiếm ~75% chiều cao => MaxY = maxVisible / 0.75
            double rawTarget = maxVisible / PeakFillRatio;

            _targetMaxValue = NiceCeil(Math.Max(1, rawTarget));

            if (DynamicMaxValue <= 0) DynamicMaxValue = _targetMaxValue;

            // smooth để MaxLabel/scale không giật
            DynamicMaxValue = DynamicMaxValue + (_targetMaxValue - DynamicMaxValue) * SmoothAlpha;
        }

        /// <summary>
        /// Update series + ticks + Y-scale theo dữ liệu window đang hiển thị
        /// </summary>
        private void UpdateAxisAndX_Smooth()
        {
            if (ViewportWidth <= 0) return;

            var (range, tickStep) = GetRangeConfig(SelectedRange);
            var nowUtc = DateTime.UtcNow;
            var windowStartUtc = nowUtc - range;

            double pxPerSec = PxPerSecond();

            DownloadData.Clear();
            UploadData.Clear();
            XPoints.Clear();

            var pts = _lastPoints
                .Where(p => p.Time.ToUniversalTime() >= windowStartUtc && p.Time.ToUniversalTime() <= nowUtc)
                .OrderBy(p => p.Time)
                .ToArray();
            _visiblePoints = pts;
            double maxVisible = 0;

            foreach (var p in pts)
            {
                var t = p.Time.ToUniversalTime();
                double x = (t - windowStartUtc).TotalSeconds * pxPerSec;

                if (x < 0) x = 0;
                if (x > ViewportWidth) x = ViewportWidth;

                XPoints.Add(x);
                DownloadData.Add(p.Download);
                UploadData.Add(p.Upload);

                double m = Math.Max(p.Download, p.Upload);
                if (m > maxVisible) maxVisible = m;
            }

            // ✅ Đây là nơi MaxY được tính đúng theo window
            UpdateYScaleTarget(maxVisible);

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

            BuildTimeTicksSmooth(windowStartUtc, nowUtc, tickStep, pxPerSec);

            OnPropertyChanged(nameof(DownloadData));
            OnPropertyChanged(nameof(UploadData));
            OnPropertyChanged(nameof(XPoints));
            OnPropertyChanged(nameof(TimeTicks));
        }

        public bool UpdateHover(double mouseXAbs, double chartHeight, double scrollOffset)
        {
            // mouseXAbs: X theo tọa độ data (pos.X + HorizontalOffset)
            // chartHeight: ChartCanvas.ActualHeight
            // scrollOffset: ChartScroller.HorizontalOffset

            if (_visiblePoints == null || _visiblePoints.Length == 0) { IsHovering = false; return false; }
            if (XPoints.Count != _visiblePoints.Length) { IsHovering = false; return false; }
            if (chartHeight <= 1) { IsHovering = false; return false; }

            // clamp theo vùng dữ liệu (ChartWidth)
            if (mouseXAbs < 0) mouseXAbs = 0;
            if (mouseXAbs > ChartWidth) mouseXAbs = ChartWidth;

            int bestIdx = 0;
            double bestDist = double.MaxValue;

            for (int i = 0; i < XPoints.Count; i++)
            {
                double d = Math.Abs(XPoints[i] - mouseXAbs);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestIdx = i;
                }
            }

            var p = _visiblePoints[bestIdx];
            double snapXAbs = XPoints[bestIdx];

            // Scale giống converter: y = height - (value/max)*height
            double max = DynamicMaxValue <= 0 ? 1 : DynamicMaxValue;

            double yDown = chartHeight - (p.Download / max) * chartHeight;
            double yUp = chartHeight - (p.Upload / max) * chartHeight;

            // clamp
            yDown = Math.Max(0, Math.Min(chartHeight, yDown));
            yUp = Math.Max(0, Math.Min(chartHeight, yUp));

            // X để vẽ trên viewport (trừ offset)
            HoverX = snapXAbs - scrollOffset;
            HoverDownloadY = yDown;
            HoverUploadY = yUp;
            IsHovering = true;

            return true;
        }

        public void ClearHover()
        {
            IsHovering = false;
        }

        public bool TryGetHoverInfo(double mouseX, out string text, out double snapX)
        {
            text = "";
            snapX = 0;

            if (_visiblePoints == null || _visiblePoints.Length == 0) return false;
            if (XPoints.Count != _visiblePoints.Length) return false;

            // clamp
            if (mouseX < 0) mouseX = 0;
            if (mouseX > ViewportWidth) mouseX = ViewportWidth;

            // tìm index gần nhất theo XPoints (O(n) nhưng n nhỏ nên ổn)
            int bestIdx = 0;
            double bestDist = double.MaxValue;

            for (int i = 0; i < XPoints.Count; i++)
            {
                double d = Math.Abs(XPoints[i] - mouseX);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestIdx = i;
                }
            }

            var p = _visiblePoints[bestIdx];
            snapX = XPoints[bestIdx];

            // format time theo range
            string timeFmt = SelectedRange == "5m" ? "HH:mm:ss" : "HH:mm";
            string timeLabel = p.Time.ToLocalTime().ToString(timeFmt);

            text =
                $"Time: {timeLabel}\n" +
                $"Download: {FormatDataRate(p.Download)}\n" +
                $"Upload: {FormatDataRate(p.Upload)}";

            return true;
        }
        private void BuildTimeTicksSmooth(DateTime windowStartUtc, DateTime nowUtc, TimeSpan tickStep, double pxPerSec)
        {
            TimeTicks.Clear();

            double labelWidth = SelectedRange == "5m" ? 95 : 70;
            string fmt = GetTickFormat(SelectedRange);

            long stepTicks = tickStep.Ticks;
            if (stepTicks <= 0) return;

            long nowTicks = nowUtc.Ticks;
            long rightTickTicks = (nowTicks / stepTicks) * stepTicks;
            var rightTickUtc = new DateTime(rightTickTicks, DateTimeKind.Utc);

            double offsetSec = (nowUtc - rightTickUtc).TotalSeconds;
            double rightX = ViewportWidth - offsetSec * pxPerSec;

            double stepPx = tickStep.TotalSeconds * pxPerSec;
            if (stepPx <= 0.1) stepPx = 0.1;

            for (int i = 0; ; i++)
            {
                var t = rightTickUtc - TimeSpan.FromTicks(stepTicks * (long)i);
                double x = rightX - i * stepPx;

                if (x < -labelWidth) break;
                if (t < windowStartUtc) break;

                double left = x - labelWidth / 2.0;

                TimeTicks.Add(new TimeTick
                {
                    Left = left,
                    LabelWidth = labelWidth,
                    Label = t.ToLocalTime().ToString(fmt)
                });
            }

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
