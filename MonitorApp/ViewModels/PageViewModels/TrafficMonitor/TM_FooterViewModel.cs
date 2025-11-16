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

        private readonly DispatcherTimer timer;
        private readonly List<string> allTimeLabels = new();

        // =============================
        // 1) DEMO SPEED + TOTAL
        // =============================

        private double downloadSpeed;   // byte/s
        public double DownloadSpeed
        {
            get => downloadSpeed;
            set { downloadSpeed = value; OnPropertyChanged(); OnPropertyChanged(nameof(DownloadSpeedText)); }
        }

        private double uploadSpeed;     // byte/s
        public double UploadSpeed
        {
            get => uploadSpeed;
            set { uploadSpeed = value; OnPropertyChanged(); OnPropertyChanged(nameof(UploadSpeedText)); }
        }

        private double downloadTotal;   // byte
        public double DownloadTotal
        {
            get => downloadTotal;
            set
            {
                downloadTotal = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DownloadTotalText));
                OnPropertyChanged(nameof(TotalUsedText));
                UpdateArcPercent();
            }
        }

        private double uploadTotal;     // byte
        public double UploadTotal
        {
            get => uploadTotal;
            set
            {
                uploadTotal = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UploadTotalText));
                OnPropertyChanged(nameof(TotalUsedText)); // cập nhật tổng
                UpdateArcPercent();
            }
        }

        public string DownloadSpeedText => FormatBytes(DownloadSpeed) + "/s";
        public string UploadSpeedText => FormatBytes(UploadSpeed) + "/s";

        public string DownloadTotalText => FormatBytes(DownloadTotal);
        public string UploadTotalText => FormatBytes(UploadTotal);

        public string TotalUsedText => FormatBytes(DownloadTotal + UploadTotal);



        // =============================
        // 2) WAN / LAN
        // =============================

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
        // 3) TIMELINE
        // =============================

        public ObservableCollection<string> TimeLabels { get; private set; } = new();

        private double smoothScrollOffset;
        public double SmoothScrollOffset
        {
            get => smoothScrollOffset;
            set { smoothScrollOffset = value; OnPropertyChanged(); }
        }

        private DateTime lastLabelTime;
        private const double LabelIntervalSeconds = 4.0;
        private const double LabelWidth = 150.0;

        // =============================
        // Constructor
        // =============================
        public TM_FooterViewModel()
        {
            // DEMO tốc độ bạn yêu cầu
            DownloadSpeed = 100 * 1024;   // 100 KB/s
            UploadSpeed = 120 * 1024;     // 120 KB/s

            WanSizeText = "1.3 MB";
            WanDownPercent = 97;
            WanFillWidth = 820;

            LanSizeText = "22.7 KB";
            LanDownPercent = 85;
            LanFillWidth = 22;

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

        private DateTime lastTotalTime = DateTime.Now;

        private void OnTimerTick(object sender, EventArgs e)
        {
            var now = DateTime.Now;

            // ====== 1) CẬP NHẬT TOTAL DỰA TRÊN THỜI GIAN THỰC ======
            double elapsedTotal = (now - lastTotalTime).TotalSeconds;
            if (elapsedTotal > 0)
            {
                DownloadTotal += DownloadSpeed * elapsedTotal;
                UploadTotal += UploadSpeed * elapsedTotal;
                lastTotalTime = now;
            }

            // ====== 2) TIMELINE ======
            double elapsedLabel = (now - lastLabelTime).TotalSeconds;

            var progress = elapsedLabel / LabelIntervalSeconds;
            SmoothScrollOffset = -(progress * LabelWidth);

            if (elapsedLabel >= LabelIntervalSeconds)
            {
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
            TimeLabels.Clear();
            const int visible = 18;

            int start = Math.Max(0, allTimeLabels.Count - visible);
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

        private void UpdateArcPercent()
        {
            double total = DownloadTotal + UploadTotal;

            if (total <= 0)
            {
                ArcDownloadPercent = 0;
                ArcUploadPercent = 0;
                ArcTotalPercent = 0;
                return;
            }

            ArcDownloadPercent = (DownloadTotal / total) * 100.0;
            ArcUploadPercent = (UploadTotal / total) * 100.0;
            ArcTotalPercent = 100.0; // luôn 100%
        }


        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
