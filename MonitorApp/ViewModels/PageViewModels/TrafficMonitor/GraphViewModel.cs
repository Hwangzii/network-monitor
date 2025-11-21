using MonitorApp.ViewModels.Stores;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

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
        private readonly DispatcherTimer _timer;
        private readonly NetworkMonitorStore _store;

        private ObservableCollection<double> downloadData;
        private ObservableCollection<double> uploadData;
        private ObservableCollection<string> timeLabels;
        private readonly List<string> allTimeLabels = new();

        private double downloadSpeed;
        private double uploadSpeed;
        private double smoothScrollOffset;
        private DateTime lastLabelTime;

        // ===========================
        // Ctor
        // ===========================
        public GraphViewModel() : this(NetworkMonitorStore.Instance) { }

        public GraphViewModel(NetworkMonitorStore store)
        {
            _store = store;

            InitializeData();

            // Timer chỉ dùng cho timeline + autoscale
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();

            // nghe dữ liệu từ store
            _store.PropertyChanged += StoreOnPropertyChanged;
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
            private set { downloadSpeed = value; OnPropertyChanged(); }
        }

        public double UploadSpeed
        {
            get => uploadSpeed;
            private set { uploadSpeed = value; OnPropertyChanged(); }
        }

        public double SmoothScrollOffset
        {
            get => smoothScrollOffset;
            private set { smoothScrollOffset = value; OnPropertyChanged(); }
        }

        public string DownloadLabel => FormatDataRate(DownloadSpeed);
        public string UploadLabel => FormatDataRate(UploadSpeed);

        // Max động để scale Y
        private double dynamicMaxValue = 100.0;
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
        // Nhận dữ liệu từ NetworkMonitorStore
        // ===========================
        private void StoreOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NetworkMonitorStore.DownloadKBps) ||
                e.PropertyName == nameof(NetworkMonitorStore.UploadKBps))
            {
                var down = _store.DownloadKBps; // KB/s
                var up = _store.UploadKBps;   // KB/s

                // cập nhật text
                DownloadSpeed = down;
                UploadSpeed = up;
                OnPropertyChanged(nameof(DownloadLabel));
                OnPropertyChanged(nameof(UploadLabel));

                // thêm điểm cho đồ thị
                DownloadData.Add(down);
                UploadData.Add(up);

                if (DownloadData.Count > MaxDataPoints)
                {
                    DownloadData.RemoveAt(0);
                    UploadData.RemoveAt(0);
                }

                OnPropertyChanged(nameof(DownloadData));
                OnPropertyChanged(nameof(UploadData));
            }
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
            _store.PropertyChanged -= StoreOnPropertyChanged;
        }

        // ===========================
        // Helpers
        // ===========================
        private string FormatDataRate(double valueKbPerSec)
        {
            // value đang là KB/s
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
