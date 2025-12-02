using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MonitorApp.Models;
using MonitorApp.Services;

namespace MonitorApp.ViewModels.PageViewModels.TrafficMonitor
{
    public class GraphViewModel : INotifyPropertyChanged
    {
        // ===========================
        // Constants
        // ===========================
        private const int MaxDataPoints = 60;
        private const int VisibleLabelCount = 18;   // Thống nhất với Footer
        private const double LabelIntervalSeconds = 4.0;
        private const double LabelWidth = 150.0;

        // ===========================
        // Fields
        // ===========================
        private readonly DispatcherTimer _timer;      // timeline + autoscale (16ms)
        private readonly DispatcherTimer _apiTimer;   // gọi API summary (1s)
        private readonly MonitorApiClient _api = new();   // client gọi backend

        private ObservableCollection<double> downloadData;
        private ObservableCollection<double> uploadData;
        private ObservableCollection<string> timeLabels;
        private readonly List<string> allTimeLabels = new();

        private double downloadSpeed;
        private double uploadSpeed;
        private double smoothScrollOffset;
        private DateTime lastLabelTime;

        // Max động để scale Y
        private double dynamicMaxValue = 100.0;

        // ===========================
        // Ctor
        // ===========================
        public GraphViewModel()
        {
            InitializeData();

            // Timer chỉ dùng cho timeline + autoscale
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();

            // Timer gọi API traffic/summary mỗi 1s
            _apiTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _apiTimer.Tick += ApiTimer_Tick;
            _apiTimer.Start();
        }

        // ===========================
        // Events
        // ===========================
        public event PropertyChangedEventHandler PropertyChanged;

        // ===========================
        // Properties
        // ===========================
        public ObservableCollection<double> DownloadData
        {
            get => downloadData;
            private set { downloadData = value; OnPropertyChanged(); }
        }

        public ObservableCollection<double> UploadData
        {
            get => uploadData;
            private set { uploadData = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> TimeLabels
        {
            get => timeLabels;
            private set { timeLabels = value; OnPropertyChanged(); }
        }

        public double DownloadSpeed
        {
            get => downloadSpeed;
            private set
            {
                downloadSpeed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DownloadLabel));
            }
        }

        public double UploadSpeed
        {
            get => uploadSpeed;
            private set
            {
                uploadSpeed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UploadLabel));
            }
        }

        public double SmoothScrollOffset
        {
            get => smoothScrollOffset;
            private set { smoothScrollOffset = value; OnPropertyChanged(); }
        }

        public string DownloadLabel => FormatDataRate(DownloadSpeed);
        public string UploadLabel => FormatDataRate(UploadSpeed);

        public double DynamicMaxValue
        {
            get => dynamicMaxValue;
            private set { dynamicMaxValue = value; OnPropertyChanged(); }
        }

        // ===========================
        // Init
        // ===========================
        private void InitializeData()
        {
            DownloadData = new ObservableCollection<double>();
            UploadData = new ObservableCollection<double>();
            TimeLabels = new ObservableCollection<string>();

            // Khởi tạo dữ liệu 0 cho đồ thị
            for (int i = 0; i < MaxDataPoints; i++)
            {
                DownloadData.Add(0);
                UploadData.Add(0);
            }

            // Khởi tạo time labels giống Footer:
            // Bắt đầu từ (now - 25 * interval) và đi tới (now - 4s)
            var now = DateTime.Now;
            lastLabelTime = now.AddSeconds(-25 * LabelIntervalSeconds);

            for (int i = 0; i < 25; i++)
            {
                var t = lastLabelTime.AddSeconds(i * LabelIntervalSeconds);
                allTimeLabels.Add(t.ToString("h:mm:ss tt"));
            }

            // Sau khi thêm 25 nhãn, lùi lại 1 khoảng để lastLabelTime = now - 4s
            lastLabelTime = lastLabelTime.AddSeconds(24 * LabelIntervalSeconds);

            UpdateVisibleTimeLabels();
        }

        // ===========================
        // GỌI API /traffic/summary mỗi 1s
        // ===========================
        private async void ApiTimer_Tick(object? sender, EventArgs e)
        {
            TrafficSummary? summary = null;

            try
            {
                summary = await _api.GetTrafficSummaryAsync();
            }
            catch
            {
                // Có lỗi kết nối thì bỏ qua tick này, tránh crash
                return;
            }

            if (summary == null)
                return;

            // Parse chuỗi "1.23 MB/s" -> KB/s (double)
            DownloadSpeed = ParseSpeed(summary.DownloadSpeed);
            UploadSpeed = ParseSpeed(summary.UploadSpeed);

            // thêm điểm cho đồ thị
            DownloadData.Add(DownloadSpeed);
            UploadData.Add(UploadSpeed);

            if (DownloadData.Count > MaxDataPoints)
            {
                DownloadData.RemoveAt(0);
                UploadData.RemoveAt(0);
            }

            OnPropertyChanged(nameof(DownloadData));
            OnPropertyChanged(nameof(UploadData));
        }

        // ===========================
        // Tick – chỉ xử lý timeline + autoscale
        // ===========================
        private void OnTimerTick(object? sender, EventArgs e)
        {
            // 1) Timeline mượt
            var now = DateTime.Now;
            var timeSinceLast = (now - lastLabelTime).TotalSeconds;
            double scrollProgress = timeSinceLast / LabelIntervalSeconds;
            SmoothScrollOffset = -scrollProgress * LabelWidth;

            if (timeSinceLast >= LabelIntervalSeconds)
            {
                lastLabelTime = lastLabelTime.AddSeconds(LabelIntervalSeconds);
                allTimeLabels.Add(lastLabelTime.ToString("h:mm:ss tt"));

                if (allTimeLabels.Count > 100)
                    allTimeLabels.RemoveAt(0);

                SmoothScrollOffset = 0;
                UpdateVisibleTimeLabels();
            }

            // 2) Auto-scale Y dựa trên điểm cuối
            if (DownloadData.Count > 0 && UploadData.Count > 0)
            {
                double maxY = Math.Max(DownloadData[^1], UploadData[^1]);
                DynamicMaxValue = Math.Max(20, maxY * 1.3);
            }
        }

        // Chỉ cập nhật phần hiển thị, không thêm/xoá nhãn ở đây
        private void UpdateVisibleTimeLabels()
        {
            TimeLabels.Clear();

            int startIndex = Math.Max(0, allTimeLabels.Count - VisibleLabelCount);
            for (int i = startIndex; i < allTimeLabels.Count; i++)
                TimeLabels.Add(allTimeLabels[i]);
        }

        // ===========================
        // Public API cho Path binding
        // ===========================
        public PointCollection GetDownloadPoints(double width, double height)
            => GetPointsFromData(DownloadData, width, height);

        public PointCollection GetUploadPoints(double width, double height)
            => GetPointsFromData(UploadData, width, height);

        private PointCollection GetPointsFromData(ObservableCollection<double> data, double width, double height)
        {
            var points = new PointCollection();

            if (data == null || data.Count == 0 || width <= 0 || height <= 0 || DynamicMaxValue <= 0)
                return points;

            double step = width / (MaxDataPoints - 1);
            double max = DynamicMaxValue;

            // Điểm bắt đầu ở dưới cùng (cho Polygon/area)
            points.Add(new Point(0, height));

            // Điểm dữ liệu
            for (int i = 0; i < data.Count; i++)
            {
                double x = i * step;
                double y = height - (data[i] / max * height);
                points.Add(new Point(x, y));
            }

            // Điểm kết thúc ở dưới cùng
            points.Add(new Point(width, height));

            return points;
        }

        public void StopMonitoring()
        {
            _timer?.Stop();
            _apiTimer?.Stop();
        }

        // ===========================
        // Helpers
        // ===========================
        // Parse "1.23 MB/s", "512 KB/s", "123 B/s", "1,23 MB/s", ...
        private double ParseSpeed(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            text = text.Replace("/s", "", StringComparison.OrdinalIgnoreCase).Trim();

            // Lấy phần số (cho dù backend trả format hơi kỳ)
            var match = Regex.Match(text, @"[\d\.,]+");
            if (!match.Success)
                return 0;

            var numberPart = match.Value.Trim();
            numberPart = numberPart.Replace(',', '.');

            if (!double.TryParse(numberPart,
                                 NumberStyles.Float,
                                 CultureInfo.InvariantCulture,
                                 out var value))
                return 0;

            var upper = text.ToUpperInvariant();

            if (upper.Contains("GB"))
                return value * 1024 * 1024;   // KB/s
            if (upper.Contains("MB"))
                return value * 1024;          // KB/s
            if (upper.Contains("KB"))
                return value;                 // KB/s
            if (upper.Contains("B"))
                return value / 1024;          // B/s -> KB/s

            // không có đơn vị -> coi như KB/s
            return value;
        }

        private string FormatDataRate(double valueKbPerSec)
        {
            if (valueKbPerSec < 1024)
                return $"{valueKbPerSec:F1} KB/s";
            if (valueKbPerSec < 1024 * 1024)
                return $"{valueKbPerSec / 1024:F1} MB/s";
            return $"{valueKbPerSec / 1024 / 1024:F1} GB/s";
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
