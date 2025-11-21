using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace MonitorApp.ViewModels.PageViewModels.TrafficMonitor
{
    public class TM_FooterViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<string> timeLabels;
        private readonly DispatcherTimer timer;
        private readonly List<string> allTimeLabels = new();

        // =============================
        // 1) DEMO SPEED + TOTAL (GLOBAL)
        // =============================

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

        // WAN totals
        private double wanDownloadTotal;
        private double wanUploadTotal;

        // LAN totals
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

        private DateTime lastTotalTime = DateTime.Now;
        private readonly Random rnd = new Random();

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

            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += OnTimerTick;
            timer.Start();
        }

        // =============================
        // Timeline helpers
        // =============================

        private void InitializeTimeline()
        {
            // giống GraphViewModel: luôn đảm bảo TimeLabels không bị null
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

        private void UpdateVisibleTimeLabels()
        {
            if (TimeLabels == null)
                TimeLabels = new ObservableCollection<string>();

            TimeLabels.Clear();

            int start = Math.Max(0, allTimeLabels.Count - VisibleLabelCount);
            for (int i = start; i < allTimeLabels.Count; i++)
                TimeLabels.Add(allTimeLabels[i]);
        }

        // =============================
        // Utils
        // =============================

        private static string FormatBytes(double b)
        {
            string[] u = { "B", "KB", "MB", "GB" };
            int i = 0;
            double v = b;
            while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
            return $"{v:0.#} {u[i]}";
        }

        /// <summary>
        /// Cập nhật donut + WAN/LAN bar dựa trên totals hiện tại.
        /// GỌI từ setter DownloadTotal/UploadTotal.
        /// </summary>
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

            // ===== 1) VÒNG CUNG (global) =====
            ArcDownloadPercent = (DownloadTotal / total) * 100.0;
            ArcUploadPercent = (UploadTotal / total) * 100.0;
            ArcTotalPercent = 100.0;

            // ===== 2) WAN BAR =====
            double wanTotal = wanDownloadTotal + wanUploadTotal;
            if (wanTotal > 0)
            {
                WanSizeText = FormatBytes(wanTotal);
                WanDownPercent = (wanDownloadTotal / wanTotal) * 100.0;
                WanFillWidth = MaxWanWidth * (wanTotal / total); // dài tỉ lệ với tổng
            }
            else
            {
                WanSizeText = "0 B";
                WanDownPercent = 0;
                WanFillWidth = 0;
            }

            // ===== 3) LAN BAR =====
            double lanTotal = lanDownloadTotal + lanUploadTotal;
            if (lanTotal > 0)
            {
                LanSizeText = FormatBytes(lanTotal);
                LanDownPercent = (lanDownloadTotal / lanTotal) * 100.0;
                LanFillWidth = MaxLanWidth * (lanTotal / total);
            }
            else
            {
                LanSizeText = "0 B";
                LanDownPercent = 0;
                LanFillWidth = 0;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
