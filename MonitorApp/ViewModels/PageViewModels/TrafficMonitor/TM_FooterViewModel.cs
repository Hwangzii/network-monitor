using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using MonitorApp.Models;
using MonitorApp.Services;

namespace MonitorApp.ViewModels.PageViewModels.TrafficMonitor
{
    public class TM_FooterViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly DispatcherTimer timer;    // timeline (mượt)
        private readonly DispatcherTimer apiTimer; // gọi API
        private readonly MonitorApiClient apiClient = new();

        // ====== TIMELINE (same as Graph) ======
        private double _viewportWidth = 800;
        public double ViewportWidth
        {
            get => _viewportWidth;
            private set { _viewportWidth = value; OnPropertyChanged(); }
        }

        private string _selectedRange = "5m";
        public string SelectedRange
        {
            get => _selectedRange;
            private set { _selectedRange = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TimeTick> TimeTicks { get; } = new();

        // ====== (GIỮ NGUYÊN PHẦN SỐ LIỆU CỦA BẠN) ======

        private double downloadSpeed; // byte/s
        public double DownloadSpeed
        {
            get => downloadSpeed;
            set { downloadSpeed = value; OnPropertyChanged(); OnPropertyChanged(nameof(DownloadSpeedText)); }
        }

        private double uploadSpeed;   // byte/s
        public double UploadSpeed
        {
            get => uploadSpeed;
            set { uploadSpeed = value; OnPropertyChanged(); OnPropertyChanged(nameof(UploadSpeedText)); }
        }

        private double downloadTotal; // byte
        public double DownloadTotal
        {
            get => downloadTotal;
            set
            {
                downloadTotal = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DownloadTotalText));
                OnPropertyChanged(nameof(TotalUsedText));
                UpdateArcAndBars();
            }
        }

        private double uploadTotal; // byte
        public double UploadTotal
        {
            get => uploadTotal;
            set
            {
                uploadTotal = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UploadTotalText));
                OnPropertyChanged(nameof(TotalUsedText));
                UpdateArcAndBars();
            }
        }

        public string DownloadSpeedText => FormatBytes(DownloadSpeed) + "/s";
        public string UploadSpeedText => FormatBytes(UploadSpeed) + "/s";
        public string DownloadTotalText => FormatBytes(DownloadTotal);
        public string UploadTotalText => FormatBytes(UploadTotal);
        public string TotalUsedText => FormatBytes(DownloadTotal + UploadTotal);

        private double wanDownloadTotal;
        private double wanUploadTotal;
        private double lanDownloadTotal;
        private double lanUploadTotal;

        private string wanSizeText = "0 B";
        public string WanSizeText
        {
            get => wanSizeText;
            set { wanSizeText = value; OnPropertyChanged(); }
        }

        private string lanSizeText = "0 B";
        public string LanSizeText
        {
            get => lanSizeText;
            set { lanSizeText = value; OnPropertyChanged(); }
        }

        private double wanDownPercent;
        public double WanDownPercent
        {
            get => wanDownPercent;
            set { wanDownPercent = ClampPercent(value); OnPropertyChanged(); OnPropertyChanged(nameof(WanUpPercent)); }
        }
        public double WanUpPercent => 100.0 - WanDownPercent;

        private double lanDownPercent;
        public double LanDownPercent
        {
            get => lanDownPercent;
            set { lanDownPercent = ClampPercent(value); OnPropertyChanged(); OnPropertyChanged(nameof(LanUpPercent)); }
        }
        public double LanUpPercent => 100.0 - LanDownPercent;

        private double wanFillWidth;
        public double WanFillWidth
        {
            get => wanFillWidth;
            set { wanFillWidth = value; OnPropertyChanged(); }
        }

        private double lanFillWidth;
        public double LanFillWidth
        {
            get => lanFillWidth;
            set { lanFillWidth = value; OnPropertyChanged(); }
        }

        private double arcDownloadPercent;
        public double ArcDownloadPercent
        {
            get => arcDownloadPercent;
            set { arcDownloadPercent = value; OnPropertyChanged(); }
        }

        private double arcUploadPercent;
        public double ArcUploadPercent
        {
            get => arcUploadPercent;
            set { arcUploadPercent = value; OnPropertyChanged(); }
        }

        private double arcTotalPercent;
        public double ArcTotalPercent
        {
            get => arcTotalPercent;
            set { arcTotalPercent = value; OnPropertyChanged(); }
        }

        private const double MaxWanWidth = 820;
        private const double MaxLanWidth = 820;

        private static double ClampPercent(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);

        public TM_FooterViewModel()
        {
            // ✅ nhận range giống Graph
            SelectedRange = NormalizeRange(TrafficRangeBus.CurrentRange);
            TrafficRangeBus.RangeChanged += r =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SelectedRange = NormalizeRange(r);
                    UpdateTicksOnly(); // đổi 5m/3h/24h -> ticks đổi ngay
                });
            };

            // timeline tick mượt (giống Graph: dùng NOW để trôi)
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            timer.Tick += (_, __) => UpdateTicksOnly();
            timer.Start();

            apiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            apiTimer.Tick += ApiTimer_Tick;
            apiTimer.Start();

            // init ticks lần đầu
            UpdateTicksOnly();
        }

        public void SetViewportWidth(double width)
        {
            if (width <= 0) return;
            ViewportWidth = width;
            UpdateTicksOnly();
        }

        // =============================
        // API /traffic/summary
        // =============================
        private async void ApiTimer_Tick(object? sender, EventArgs e)
        {
            TrafficSummary? summary = null;

            try
            {
                summary = await apiClient.GetTrafficSummaryAsync();
            }
            catch
            {
                return;
            }

            if (summary == null)
                return;

            double downKb = ParseSpeedToKB(summary.DownloadSpeed);
            double upKb = ParseSpeedToKB(summary.UploadSpeed);

            DownloadSpeed = downKb * 1024.0; // byte/s
            UploadSpeed = upKb * 1024.0;     // byte/s

            DownloadTotal = ParseSizeToBytes(summary.DownloadTotal);
            UploadTotal = ParseSizeToBytes(summary.UploadTotal);

            double wanBytes = ParseSizeToBytes(summary.WanUsage);
            double lanBytes = ParseSizeToBytes(summary.LanUsage);

            double totalBytes = DownloadTotal + UploadTotal;
            double wanLanSum = wanBytes + lanBytes;

            if (wanLanSum <= 0 || totalBytes <= 0)
            {
                wanDownloadTotal = wanUploadTotal = lanDownloadTotal = lanUploadTotal = 0;
            }
            else
            {
                double wanShare = wanBytes / wanLanSum;
                double lanShare = 1.0 - wanShare;

                double ratioSum = summary.DownloadRatio + summary.UploadRatio;
                if (ratioSum <= 0) ratioSum = 1;
                double downShare = summary.DownloadRatio / ratioSum;
                double upShare = 1.0 - downShare;

                double totalDownBytes = totalBytes * downShare;
                double totalUpBytes = totalBytes * upShare;

                wanDownloadTotal = totalDownBytes * wanShare;
                lanDownloadTotal = totalDownBytes * lanShare;
                wanUploadTotal = totalUpBytes * wanShare;
                lanUploadTotal = totalUpBytes * lanShare;
            }

            UpdateArcAndBars();
        }

        // =============================
        // TIMELINE (same algorithm as Graph)
        // =============================
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

        private void UpdateTicksOnly()
        {
            if (ViewportWidth <= 0) return;

            var (range, tickStep) = GetRangeConfig(SelectedRange);
            var nowUtc = DateTime.UtcNow;
            var windowStartUtc = nowUtc - range;
            double pxPerSec = PxPerSecond();

            BuildTimeTicksSmooth(windowStartUtc, nowUtc, tickStep, pxPerSec);

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

                TimeTicks.Add(new TimeTick
                {
                    Left = x - labelWidth / 2.0,
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

        // =============================
        // Donut + bars (giữ nguyên)
        // =============================
        private void UpdateArcAndBars()
        {
            double total = DownloadTotal + UploadTotal;

            if (total <= 0)
            {
                ArcDownloadPercent = 0;
                ArcUploadPercent = 0;
                ArcTotalPercent = 0;

                WanSizeText = "0 B";
                LanSizeText = "0 B";

                WanDownPercent = 0;
                LanDownPercent = 0;

                WanFillWidth = 0;
                LanFillWidth = 0;
                return;
            }

            ArcDownloadPercent = (DownloadTotal / total) * 100.0;
            ArcUploadPercent = (UploadTotal / total) * 100.0;
            ArcTotalPercent = 100.0;

            double wanTotal = wanDownloadTotal + wanUploadTotal;
            double lanTotal = lanDownloadTotal + lanUploadTotal;

            WanSizeText = wanTotal > 0 ? FormatBytes(wanTotal) : "0 B";
            LanSizeText = lanTotal > 0 ? FormatBytes(lanTotal) : "0 B";

            WanFillWidth = wanTotal > 0 ? MaxWanWidth * (wanTotal / total) : 0;
            LanFillWidth = lanTotal > 0 ? MaxLanWidth * (lanTotal / total) : 0;

            double globalDownPercent = ClampPercent((DownloadTotal / total) * 100.0);
            WanDownPercent = globalDownPercent;
            LanDownPercent = globalDownPercent;
        }

        // =============================
        // Utils parse/format (giữ nguyên)
        // =============================
        private static double ParseSizeToBytes(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            text = text.Trim();
            var match = Regex.Match(text, @"[\d\.,]+");
            if (!match.Success)
                return 0;

            var numberPart = match.Value.Replace(',', '.');

            if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return 0;

            var upper = text.ToUpperInvariant();

            if (upper.Contains("GB")) return value * 1024 * 1024 * 1024;
            if (upper.Contains("MB")) return value * 1024 * 1024;
            if (upper.Contains("KB")) return value * 1024;
            return value;
        }

        private static double ParseSpeedToKB(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            text = text.Replace("/s", "", StringComparison.OrdinalIgnoreCase).Trim();

            var match = Regex.Match(text, @"[\d\.,]+");
            if (!match.Success)
                return 0;

            var numberPart = match.Value.Replace(',', '.');

            if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return 0;

            var upper = text.ToUpperInvariant();

            if (upper.Contains("GB")) return value * 1024 * 1024;
            if (upper.Contains("MB")) return value * 1024;
            if (upper.Contains("KB")) return value;
            if (upper.Contains("B")) return value / 1024;

            return value;
        }

        private static string FormatBytes(double b)
        {
            string[] u = { "B", "KB", "MB", "GB" };
            int i = 0;
            double v = b;
            while (v >= 1024 && i < u.Length - 1)
            {
                v /= 1024;
                i++;
            }
            return $"{v:0.#} {u[i]}";
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null!)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
