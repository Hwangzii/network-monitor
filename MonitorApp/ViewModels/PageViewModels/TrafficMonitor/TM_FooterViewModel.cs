using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using MonitorApp.Models;
using MonitorApp.Services;

namespace MonitorApp.ViewModels.PageViewModels.TrafficMonitor
{
    public class TM_FooterViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<string> timeLabels;
        private readonly DispatcherTimer timer;        // timeline
        private readonly DispatcherTimer apiTimer;     // gọi API
        private readonly MonitorApiClient apiClient = new();

        // ✅ Thêm Stopwatch để sync với GraphViewModel
        private readonly Stopwatch _labelStopwatch = Stopwatch.StartNew();
        
        // ✅ Flag để kiểm soát cập nhật nhãn chỉ 1 lần/4s
        private int _lastLabelUpdateIndex = -1;

        private readonly List<string> allTimeLabels = new();

        // =============================
        // 1) SPEED + TOTAL (GLOBAL)
        // =============================

        // DownloadSpeed / UploadSpeed lưu theo BYTE/S (để FormatBytes() ra đúng đơn vị)
        private double downloadSpeed;   // byte/s (global)
        public double DownloadSpeed
        {
            get => downloadSpeed;
            set { downloadSpeed = value; OnPropertyChanged(); OnPropertyChanged(nameof(DownloadSpeedText)); }
        }

        private double uploadSpeed;     // byte/s (global)
        public double UploadSpeed
        {
            get => uploadSpeed;
            set { uploadSpeed = value; OnPropertyChanged(); OnPropertyChanged(nameof(UploadSpeedText)); }
        }

        private double downloadTotal;   // byte (global)
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

        private double uploadTotal;     // byte (global)
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

        // =============================
        // 2) WAN / LAN (mỗi cái có down + up riêng)
        // =============================

        // WAN totals (bytes)
        private double wanDownloadTotal;
        private double wanUploadTotal;

        // LAN totals (bytes)
        private double lanDownloadTotal;
        private double lanUploadTotal;

        private string wanSizeText;
        public string WanSizeText
        {
            get => wanSizeText;
            set { wanSizeText = value; OnPropertyChanged(); }
        }

        private string lanSizeText;
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

        // =============================
        // 3) ARC (donut)
        // =============================

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

        private static double ClampPercent(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);

        // =============================
        // 4) TIMELINE
        // =============================

        public ObservableCollection<string> TimeLabels
        {
            get => timeLabels;
            private set { timeLabels = value; OnPropertyChanged(); }
        }

        private double smoothScrollOffset;
        public double SmoothScrollOffset
        {
            get => smoothScrollOffset;
            set { smoothScrollOffset = value; OnPropertyChanged(); }
        }

        private DateTime lastLabelTime;
        private const double LabelIntervalSeconds = 4.0;
        private const double LabelWidth = 150.0;
        private const int VisibleLabelCount = 18;   // đồng bộ với GraphViewModel

        // bar max width (khớp XAML)
        private const double MaxWanWidth = 820;
        private const double MaxLanWidth = 820;

        // =============================
        // Constructor
        // =============================
        public TM_FooterViewModel()
        {
            // khởi tạo totals = 0
            DownloadTotal = 0;
            UploadTotal = 0;

            wanDownloadTotal = 0;
            wanUploadTotal = 0;
            lanDownloadTotal = 0;
            lanUploadTotal = 0;

            WanSizeText = "0 B";
            LanSizeText = "0 B";
            WanFillWidth = 0;
            LanFillWidth = 0;

            InitializeTimeline();

            // timer cho timeline (16ms)
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += OnTimerTick;
            timer.Start();

            // timer gọi API summary (1s)
            apiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            apiTimer.Tick += ApiTimer_Tick;
            apiTimer.Start();
        }

        // =============================
        // GỌI API /traffic/summary
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
                // lỗi mạng thì bỏ qua tick này, tránh crash
                return;
            }

            if (summary == null)
                return;

            // 1) SPEED: Graph dùng KB/s, footer dùng BYTES/s nhưng text phải giống nhau
            //    => parse ra KB/s rồi *1024 để ra BYTES/s, FormatBytes sẽ ra cùng số KB.
            double downKb = ParseSpeedToKB(summary.DownloadSpeed);
            double upKb = ParseSpeedToKB(summary.UploadSpeed);

            DownloadSpeed = downKb * 1024.0; // byte/s
            UploadSpeed = upKb * 1024.0; // byte/s

            // 2) GLOBAL TOTAL (bytes)
            DownloadTotal = ParseSizeToBytes(summary.DownloadTotal);
            UploadTotal = ParseSizeToBytes(summary.UploadTotal);

            // 3) WAN / LAN TOTAL (bytes)
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
                // Tỉ lệ WAN vs LAN trên tổng
                double wanShare = wanBytes / wanLanSum;
                double lanShare = 1.0 - wanShare;

                // Tỉ lệ download vs upload
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

            // Cập nhật donut + bars
            UpdateArcAndBars();
        }

        // =============================
        // Timeline helpers
        // =============================

        private void InitializeTimeline()
        {
            if (TimeLabels == null)
                TimeLabels = new ObservableCollection<string>();

            TimeLabels.Clear();
            allTimeLabels.Clear();

            var now = DateTime.Now;
            lastLabelTime = now.AddSeconds(-25 * LabelIntervalSeconds);

            for (int i = 0; i < 25; i++)
            {
                var t = lastLabelTime.AddSeconds(i * LabelIntervalSeconds);
                allTimeLabels.Add(t.ToString("h:mm:ss tt"));
            }

            lastLabelTime = lastLabelTime.AddSeconds(24 * LabelIntervalSeconds);

            UpdateVisibleTimeLabels();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            // ✅ TIMELINE: Tịnh tiến mượt mà - từ phải sang trái (dữ liệu cũ được đẩy sang trái)
            double elapsedSeconds = _labelStopwatch.Elapsed.TotalSeconds;
            double progress = (elapsedSeconds % LabelIntervalSeconds) / LabelIntervalSeconds;
            
            // ✅ Scroll offset: từ 0 → -150px (phải sang trái)
            // progress = 0: offset = 0 (timeline ở vị trí ban đầu)
            // progress = 1: offset = -150 (timeline đẩy sang trái để nhãn mới xuất hiện)
            double scrollOffset = -progress * LabelWidth;
            SmoothScrollOffset = scrollOffset;

            // ✅ CHỈ UPDATE 1 LẦN/4S (dùng index, không dùng time range)
            int currentUpdateIndex = (int)(elapsedSeconds / LabelIntervalSeconds);
            
            if (currentUpdateIndex > _lastLabelUpdateIndex)
            {
                _lastLabelUpdateIndex = currentUpdateIndex;
                lastLabelTime = lastLabelTime.AddSeconds(LabelIntervalSeconds);
                allTimeLabels.Add(lastLabelTime.ToString("h:mm:ss tt"));
                if (allTimeLabels.Count > 200)
                    allTimeLabels.RemoveAt(0);
                UpdateVisibleTimeLabels();
            }
        }

        private void UpdateVisibleTimeLabels()
        {
            if (TimeLabels == null)
                TimeLabels = new ObservableCollection<string>();

            TimeLabels.Clear();

            // ✅ Hiển thị với buffer phía trước - thêm 3 nhãn dự phòng
            // Để khi scroll sang trái không bị lặp lại
            int bufferLabels = 3;
            int totalLabelsToShow = VisibleLabelCount + bufferLabels;

            int start = Math.Max(0, allTimeLabels.Count - totalLabelsToShow);
            for (int i = start; i < allTimeLabels.Count; i++)
                TimeLabels.Add(allTimeLabels[i]);
        }

        // =============================
        // Utils parse/format
        // =============================

        // parse "1.43 GB" / "622.02 MB" -> BYTES
        private static double ParseSizeToBytes(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            text = text.Trim();
            var match = Regex.Match(text, @"[\d\.,]+");
            if (!match.Success)
                return 0;

            var numberPart = match.Value.Replace(',', '.');

            if (!double.TryParse(numberPart,
                                 NumberStyles.Float,
                                 CultureInfo.InvariantCulture,
                                 out var value))
                return 0;

            var upper = text.ToUpperInvariant();

            if (upper.Contains("GB")) return value * 1024 * 1024 * 1024;
            if (upper.Contains("MB")) return value * 1024 * 1024;
            if (upper.Contains("KB")) return value * 1024;
            // B hoặc không có đơn vị
            return value;
        }

        // parse "947.2 KB/s", "1.2 MB/s" -> KB/s (để dùng chung với Graph logic)
        private static double ParseSpeedToKB(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            text = text.Replace("/s", "", StringComparison.OrdinalIgnoreCase).Trim();

            var match = Regex.Match(text, @"[\d\.,]+");
            if (!match.Success)
                return 0;

            var numberPart = match.Value.Replace(',', '.');

            if (!double.TryParse(numberPart,
                                 NumberStyles.Float,
                                 CultureInfo.InvariantCulture,
                                 out var value))
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

        /// <summary>
        /// Cập nhật donut + WAN/LAN bar dựa trên totals hiện tại.
        /// GỌI từ setter DownloadTotal/UploadTotal và ApiTimer_Tick.
        /// </summary>
        private void UpdateArcAndBars()
        {
            double total = DownloadTotal + UploadTotal;

            if (total <= 0)
            {
                // Donut
                ArcDownloadPercent = 0;
                ArcUploadPercent = 0;
                ArcTotalPercent = 0;

                // Text
                WanSizeText = "0 B";
                LanSizeText = "0 B";

                // Màu bar
                WanDownPercent = 0;
                LanDownPercent = 0;

                // Độ dài bar
                WanFillWidth = 0;
                LanFillWidth = 0;
                return;
            }

            // ===== 1) VÒNG CUNG (global) =====
            ArcDownloadPercent = (DownloadTotal / total) * 100.0;
            ArcUploadPercent = (UploadTotal / total) * 100.0;
            ArcTotalPercent = 100.0;

            // ===== 2) TỔNG THEO WAN / LAN =====
            double wanTotal = wanDownloadTotal + wanUploadTotal;
            double lanTotal = lanDownloadTotal + lanUploadTotal;

            WanSizeText = wanTotal > 0 ? FormatBytes(wanTotal) : "0 B";
            LanSizeText = lanTotal > 0 ? FormatBytes(lanTotal) : "0 B";

            // Độ dài thanh: tỉ lệ WAN/LAN so với toàn bộ traffic
            WanFillWidth = wanTotal > 0 ? MaxWanWidth * (wanTotal / total) : 0;
            LanFillWidth = lanTotal > 0 ? MaxLanWidth * (lanTotal / total) : 0;

            // ===== 3) TỈ LỆ MÀU VÀNG/HỒNG (DOWN/UP) =====
            // Dùng tỉ lệ download / upload GLOBAL cho cả WAN và LAN,
            // giúp pattern màu giống hệt nhau (như hình mẫu).
            double globalDownPercent = ClampPercent((DownloadTotal / total) * 100.0);

            WanDownPercent = globalDownPercent;
            LanDownPercent = globalDownPercent;
            // WanUpPercent & LanUpPercent tự tính = 100 - DownPercent
        }
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
