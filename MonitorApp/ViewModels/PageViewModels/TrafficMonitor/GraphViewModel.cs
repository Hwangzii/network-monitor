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
        // =========================
        // Fields / Const
        // =========================
        private readonly MonitorApiClient _api = new();

        private readonly DispatcherTimer _dataTimer;   // gọi API theo range
        private readonly DispatcherTimer _axisTimer;   // chạy trục thời gian mượt

        private TrafficChartPoint[] _lastPoints = Array.Empty<TrafficChartPoint>();
        private TrafficChartPoint[] _visiblePoints = Array.Empty<TrafficChartPoint>();

        // MaxY trục sao cho đỉnh cao nhất chiếm ~95% chiều cao chart
        private const double PeakFillRatio = 0.95;

        // mượt scale Y
        private const double SmoothAlpha = 0.18;

        private double _targetMaxValue = 1;

        // Window/time mapping (để đổi X -> Time)
        private DateTime _windowStartUtc;
        private DateTime _nowUtc;
        private DateTime? _selStartUtc;
        private DateTime? _selEndUtc;

        private double _pxPerSec;

        // =========================
        // INotifyPropertyChanged
        // =========================
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // =========================
        // Bindable Properties (Layout)
        // =========================
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

        // =========================
        // Bindable Properties (Crosshair)
        // =========================
        private bool _isHovering;
        public bool IsHovering
        {
            get => _isHovering;
            set { _isHovering = value; OnPropertyChanged(); }
        }

        private double _hoverX;
        public double HoverX
        {
            get => _hoverX;
            set { _hoverX = value; OnPropertyChanged(); }
        }

        private double _hoverDownloadY;
        public double HoverDownloadY
        {
            get => _hoverDownloadY;
            set { _hoverDownloadY = value; OnPropertyChanged(); }
        }

        private double _hoverUploadY;
        public double HoverUploadY
        {
            get => _hoverUploadY;
            set { _hoverUploadY = value; OnPropertyChanged(); }
        }

        // =========================
        // Bindable Properties (Y scale)
        // =========================
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

        // backend: kb/s (kilobit/s) => label bit-rate
        public string MaxLabel => FormatDataRateKbps(DynamicMaxValue);

        // =========================
        // Series / Axis data
        // =========================
        public ObservableCollection<double> DownloadData { get; } = new(); // kb/s
        public ObservableCollection<double> UploadData { get; } = new();   // kb/s
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

        // =========================
        // Selection state
        // =========================
        private bool _isSelecting;
        public bool IsSelecting
        {
            get => _isSelecting;
            set { _isSelecting = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsSelectionVisible)); }
        }

        private bool _hasSelection;
        public bool HasSelection
        {
            get => _hasSelection;
            set { _hasSelection = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsSelectionVisible)); }
        }

        public bool IsSelectionVisible => IsSelecting || HasSelection;

        private double _selectionLeft;
        public double SelectionLeft
        {
            get => _selectionLeft;
            set { _selectionLeft = value; OnPropertyChanged(); }
        }

        private double _selectionWidth;
        public double SelectionWidth
        {
            get => _selectionWidth;
            set { _selectionWidth = value; OnPropertyChanged(); }
        }

        private string _selectionSummary = "";
        public string SelectionSummary
        {
            get => _selectionSummary;
            set { _selectionSummary = value; OnPropertyChanged(); }
        }

        private double _selStartXAbs;
        private double _selEndXAbs;

        // =========================
        // ctor
        // =========================
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

            _axisTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _axisTimer.Tick += (_, __) => UpdateAxisAndX_Smooth();
            _axisTimer.Start();

            _ = LoadChartAsync(SelectedRange);
        }

        // =========================
        // Public API
        // =========================
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

            UpdateAxisAndX_Smooth();
        }

        // =========================
        // Range config / ticks
        // =========================
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

        // =========================
        // Load data
        // =========================
        private async Task LoadChartAsync(string range)
        {
            try
            {
                var resp = await _api.GetTrafficChartAsync(range);
                if (resp?.Points == null) return;

                _lastPoints = resp.Points.ToArray();
                UpdateAxisAndX_Smooth();
            }
            catch
            {
                // ignore
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

        // =========================
        // Y-scale helpers
        // =========================
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

            double rawTarget = maxVisible / PeakFillRatio;
            _targetMaxValue = NiceCeil(Math.Max(1, rawTarget));

            if (DynamicMaxValue <= 0) DynamicMaxValue = _targetMaxValue;

            DynamicMaxValue = DynamicMaxValue + (_targetMaxValue - DynamicMaxValue) * SmoothAlpha;
        }

        // =========================
        // Axis + series build
        // =========================
        private void UpdateSelectionVisualFromTime()
        {
            if (_selStartUtc == null || _selEndUtc == null)
            {
                SelectionWidth = 0;
                return;
            }

            double x1 = TimeUtcToXAbs(_selStartUtc.Value);
            double x2 = TimeUtcToXAbs(_selEndUtc.Value);

            double left = Math.Min(x1, x2);
            double right = Math.Max(x1, x2);

            SelectionLeft = left;
            SelectionWidth = Math.Max(0, right - left);

            // nếu hoàn toàn ngoài viewport thì ẩn (tuỳ bạn)
            if (SelectionWidth <= 0.5)
            {
                // vẫn giữ HasSelection=true để summary còn, nhưng không vẽ
                // nếu muốn ẩn hẳn thì: HasSelection=false;
            }
        }

        private void UpdateAxisAndX_Smooth()
        {
            if (ViewportWidth <= 0) return;

            var (range, tickStep) = GetRangeConfig(SelectedRange);
            var nowUtc = DateTime.UtcNow;
            var windowStartUtc = nowUtc - range;
            double pxPerSec = PxPerSecond();

            _nowUtc = nowUtc;
            _windowStartUtc = windowStartUtc;
            _pxPerSec = pxPerSec;

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
                DownloadData.Add(p.Download); // kb/s
                UploadData.Add(p.Upload);     // kb/s

                double m = Math.Max(p.Download, p.Upload);
                if (m > maxVisible) maxVisible = m;
            }

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

            // ✅ nếu đã chọn xong, vùng chọn sẽ chạy theo chart/timeline
            if (HasSelection && !IsSelecting && _selStartUtc != null && _selEndUtc != null)
            {
                UpdateSelectionVisualFromTime();
            }


            OnPropertyChanged(nameof(DownloadData));
            OnPropertyChanged(nameof(UploadData));
            OnPropertyChanged(nameof(XPoints));
            OnPropertyChanged(nameof(TimeTicks));
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

        // =========================
        // Hover (crosshair)
        // =========================
        public bool UpdateHover(double mouseXAbs, double chartHeight, double scrollOffset)
        {
            if (_visiblePoints == null || _visiblePoints.Length == 0) { IsHovering = false; return false; }
            if (XPoints.Count != _visiblePoints.Length) { IsHovering = false; return false; }
            if (chartHeight <= 1) { IsHovering = false; return false; }

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

            double max = DynamicMaxValue <= 0 ? 1 : DynamicMaxValue;
            double yDown = chartHeight - (p.Download / max) * chartHeight;
            double yUp = chartHeight - (p.Upload / max) * chartHeight;

            yDown = Math.Max(0, Math.Min(chartHeight, yDown));
            yUp = Math.Max(0, Math.Min(chartHeight, yUp));

            HoverX = snapXAbs - scrollOffset;
            HoverDownloadY = yDown;
            HoverUploadY = yUp;
            IsHovering = true;

            return true;
        }

        private double TimeUtcToXAbs(DateTime utc)
        {
            if (_pxPerSec <= 0) return 0;
            double x = (utc - _windowStartUtc).TotalSeconds * _pxPerSec;
            if (x < 0) x = 0;
            if (x > ViewportWidth) x = ViewportWidth;
            return x;
        }


        public void ClearHover() => IsHovering = false;

        public bool TryGetHoverInfo(double mouseXAbs, out string text, out double snapXAbs)
        {
            text = "";
            snapXAbs = 0;

            if (_visiblePoints == null || _visiblePoints.Length == 0) return false;
            if (XPoints.Count != _visiblePoints.Length) return false;

            if (mouseXAbs < 0) mouseXAbs = 0;
            if (mouseXAbs > ViewportWidth) mouseXAbs = ViewportWidth;

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
            snapXAbs = XPoints[bestIdx];

            string timeFmt = SelectedRange == "5m" ? "HH:mm:ss" : "HH:mm";
            string timeLabel = p.Time.ToLocalTime().ToString(timeFmt);

            text =
                $"Time: {timeLabel}\n" +
                $"Download: {FormatDataRateKbps(p.Download)}\n" +
                $"Upload: {FormatDataRateKbps(p.Upload)}";

            return true;
        }

        // =========================
        // Selection (drag)
        // =========================
        public void BeginSelection(double xAbs)
        {
            IsSelecting = true;
            HasSelection = false;

            _selStartXAbs = ClampXAbs(xAbs);
            _selEndXAbs = _selStartXAbs;

            UpdateSelectionVisual();
            SelectionSummary = "";
        }

        public void UpdateSelection(double xAbs)
        {
            if (!IsSelecting) return;

            _selEndXAbs = ClampXAbs(xAbs);
            UpdateSelectionVisual();
            ComputeSelectionTotals(); // live update
        }

        public void EndSelection(double xAbs)
        {
            if (!IsSelecting) return;

            _selEndXAbs = ClampXAbs(xAbs);
            IsSelecting = false;

            if (Math.Abs(_selEndXAbs - _selStartXAbs) < 3)
            {
                HasSelection = false;
                SelectionWidth = 0;
                SelectionSummary = "";
                _selStartUtc = _selEndUtc = null;
                return;
            }

            // ✅ chốt theo time tuyệt đối
            var t1 = XAbsToTimeUtc(Math.Min(_selStartXAbs, _selEndXAbs));
            var t2 = XAbsToTimeUtc(Math.Max(_selStartXAbs, _selEndXAbs));
            _selStartUtc = t1;
            _selEndUtc = t2;

            HasSelection = true;

            // ✅ update visual theo time để “chạy” theo chart
            UpdateSelectionVisualFromTime();

            ComputeSelectionTotals();
        }


        public void ClearSelection()
        {
            IsSelecting = false;
            HasSelection = false;
            SelectionWidth = 0;
            SelectionSummary = "";
            _selStartUtc = _selEndUtc = null;
        }


        private double ClampXAbs(double xAbs)
        {
            if (xAbs < 0) return 0;
            if (xAbs > ViewportWidth) return ViewportWidth;
            return xAbs;
        }

        private void UpdateSelectionVisual()
        {
            double leftAbs = Math.Min(_selStartXAbs, _selEndXAbs);
            double rightAbs = Math.Max(_selStartXAbs, _selEndXAbs);

            SelectionLeft = leftAbs;
            SelectionWidth = Math.Max(0, rightAbs - leftAbs);
        }

        private DateTime XAbsToTimeUtc(double xAbs)
        {
            if (_pxPerSec <= 0) return _nowUtc;
            return _windowStartUtc + TimeSpan.FromSeconds(xAbs / _pxPerSec);
        }

        private double GetMaxGapSeconds()
        {
            // cho phép gap tối đa = 3 * tickStep để tránh “phình” khi mất mẫu
            var (_, tickStep) = GetRangeConfig(SelectedRange);
            return Math.Max(5, tickStep.TotalSeconds * 3);
        }

        private void ComputeSelectionTotals()
        {
            if (_visiblePoints == null || _visiblePoints.Length < 2)
            {
                SelectionSummary = "";
                return;
            }

            var t1 = XAbsToTimeUtc(Math.Min(_selStartXAbs, _selEndXAbs));
            var t2 = XAbsToTimeUtc(Math.Max(_selStartXAbs, _selEndXAbs));
            if (t2 <= t1) { SelectionSummary = ""; return; }

            var pts = _visiblePoints
                .Select(p => new { T = p.Time.ToUniversalTime(), p.Download, p.Upload })
                .OrderBy(p => p.T)
                .ToArray();

            double downKb = 0, upKb = 0; // kilobit
            double maxGapSec = GetMaxGapSeconds();

            for (int i = 0; i < pts.Length - 1; i++)
            {
                var a = pts[i];
                var b = pts[i + 1];

                var segStart = a.T;
                var segEnd = b.T;

                if (segEnd <= t1 || segStart >= t2) continue;

                double segLen = (segEnd - segStart).TotalSeconds;
                if (segLen <= 0) continue;

                // skip gap lớn
                if (segLen > maxGapSec) continue;

                var oStart = segStart < t1 ? t1 : segStart;
                var oEnd = segEnd > t2 ? t2 : segEnd;

                double Lerp(double v0, double v1, double alpha) => v0 + (v1 - v0) * alpha;

                double a0 = (oStart - segStart).TotalSeconds / segLen;
                double a1 = (oEnd - segStart).TotalSeconds / segLen;

                double down0 = Lerp(a.Download, b.Download, a0);
                double down1 = Lerp(a.Download, b.Download, a1);
                double up0 = Lerp(a.Upload, b.Upload, a0);
                double up1 = Lerp(a.Upload, b.Upload, a1);

                double dt = (oEnd - oStart).TotalSeconds;

                downKb += (down0 + down1) * 0.5 * dt; // (kb/s)*s = kb
                upKb += (up0 + up1) * 0.5 * dt;
            }

            var dur = t2 - t1;
            var localStart = t1.ToLocalTime();
            var localEnd = t2.ToLocalTime();

            SelectionSummary =
                $"{localStart:HH:mm:ss} → {localEnd:HH:mm:ss} ({FormatDuration(dur)})\n" +
                $"Down {FormatDataSizeFromKilobit(downKb)} | Up {FormatDataSizeFromKilobit(upKb)} | Total {FormatDataSizeFromKilobit(downKb + upKb)}";
        }

        // =========================
        // Formatting (backend kb/s)
        // =========================
        private static string FormatDataRateKbps(double kbps)
        {
            // decimal bit-rate: 1000 Kbps = 1 Mbps
            if (kbps < 1000) return $"{kbps:F1} Kbps";
            if (kbps < 1000 * 1000) return $"{kbps / 1000:F1} Mbps";
            return $"{kbps / 1000 / 1000:F1} Gbps";
        }

        private static string FormatDataSizeFromKilobit(double kilobit)
        {
            // kilobit (Kb) -> bytes
            // 1 Kb = 1000 bits; bytes = bits / 8
            double bytes = (kilobit * 1000.0) / 8.0;

            // decimal bytes: 1000 B = 1 KB, 1000 KB = 1 MB, 1000 MB = 1 GB
            const double B_PER_KB = 1000.0;
            const double B_PER_MB = 1000.0 * 1000.0;
            const double B_PER_GB = 1000.0 * 1000.0 * 1000.0;

            if (bytes < B_PER_KB) return $"{bytes:F0} B";
            if (bytes < B_PER_MB) return $"{bytes / B_PER_KB:F2} KB";
            if (bytes < B_PER_GB) return $"{bytes / B_PER_MB:F2} MB";
            return $"{bytes / B_PER_GB:F2} GB";
        }

        private static string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
            if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
            return $"{ts.TotalSeconds:F0}s";
        }
    }
}
