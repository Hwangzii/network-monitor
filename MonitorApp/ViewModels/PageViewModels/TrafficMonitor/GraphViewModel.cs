using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MonitorApp.ViewModels.PageViewModels.TrafficMonitor
{
    public class GraphViewModel : INotifyPropertyChanged
    {
        // ===========================
        // Constants
        // ===========================
        private const int MaxDataPoints = 60;
        private const double MaxValue = 100.0;
        private const int VisibleLabelCount = 18;   // Thống nhất với Footer
        private const double LabelIntervalSeconds = 4.0;
        private const double LabelWidth = 150.0;

        // ===========================
        // Fields
        // ===========================
        private readonly DispatcherTimer timer;
        private readonly Random random = new Random();

        private ObservableCollection<double> downloadData;
        private ObservableCollection<double> uploadData;
        private ObservableCollection<string> timeLabels;
        private readonly List<string> allTimeLabels = new List<string>();

        private double downloadSpeed;
        private double uploadSpeed;
        private double smoothScrollOffset;

        private DateTime lastLabelTime;
        private int updateCounter = 0;

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

        public string DownloadLabel
        {
            get => FormatDataRate(DownloadSpeed);
        }

        public string UploadLabel
        {
            get => FormatDataRate(UploadSpeed);
        }

        private string FormatDataRate(double value)
        {
            if (value < 1024)
                return $"{value:F1} KB/s";
            if (value < 1024 * 1024)
                return $"{value / 1024:F1} MB/s";
            return $"{value / 1024 / 1024:F1} GB/s";
        }


        // ===========================
        // Ctor
        // ===========================
        public GraphViewModel()
        {
            InitializeData();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            timer.Tick += OnTimerTick;
            timer.Start();

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
        // Tick
        // ===========================
        private double demoT = 0;

        private void OnTimerTick(object sender, EventArgs e)
        {
            updateCounter++;

            // Cập nhật dữ liệu đồ thị mỗi 250ms
            if (updateCounter % 16 == 0)
            {
                demoT += 0.15;

                // ====== DEMO DATA (không random) ======
                double newDownload = 50 + Math.Sin(demoT) * 40;   // sóng lớn
                double newUpload = 20 + Math.Cos(demoT * 1.7) * 15;  // sóng nhỏ

                // Giới hạn không âm
                newDownload = Math.Max(0, newDownload);
                newUpload = Math.Max(0, newUpload);

                // Thêm vào list
                DownloadData.Add(newDownload);
                UploadData.Add(newUpload);

                if (DownloadData.Count > MaxDataPoints)
                {
                    DownloadData.RemoveAt(0);
                    UploadData.RemoveAt(0);
                }

                // Update UI
                OnPropertyChanged(nameof(DownloadData));
                OnPropertyChanged(nameof(UploadData));

                // Speed hiển thị
                DownloadSpeed = newDownload;
                UploadSpeed = newUpload;

                OnPropertyChanged(nameof(DownloadLabel));
                OnPropertyChanged(nameof(UploadLabel));
                // =======================================
            }

            // Tính smooth scroll theo thời gian
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

            // Auto-scale giống GlassWire
            double maxY = Math.Max(
                DownloadData[^1],
                UploadData[^1]
            );

            DynamicMaxValue = Math.Max(20, maxY * 1.3);
        }

        // Chỉ cập nhật phần hiển thị, không thêm/xoá nhãn ở đây
        private void UpdateVisibleTimeLabels()
        {
            TimeLabels.Clear();

            int startIndex = Math.Max(0, allTimeLabels.Count - VisibleLabelCount);
            for (int i = startIndex; i < allTimeLabels.Count; i++)
            {
                TimeLabels.Add(allTimeLabels[i]);
            }
        }

        // ===========================
        // Public API cho Polyline binding
        // ===========================
        public PointCollection GetDownloadPoints(double width, double height)
            => GetPointsFromData(DownloadData, width, height);

        public PointCollection GetUploadPoints(double width, double height)
            => GetPointsFromData(UploadData, width, height);

        private PointCollection GetPointsFromData(ObservableCollection<double> data, double width, double height)
        {
            var points = new PointCollection();

            if (data == null || data.Count == 0 || width <= 0 || height <= 0)
                return points;

            double step = width / (MaxDataPoints - 1);

            // Điểm bắt đầu ở dưới cùng (nếu dùng cho Polygon/Area)
            points.Add(new Point(0, height));

            // Điểm dữ liệu
            for (int i = 0; i < data.Count; i++)
            {
                double x = i * step;
                double y = height - (data[i] / MaxValue * height);
                points.Add(new Point(x, y));
            }

            // Điểm kết thúc ở dưới cùng
            points.Add(new Point(width, height));

            return points;
        }

        public void StopMonitoring()
        {
            timer?.Stop();
        }
        private double dynamicMaxValue;
        public double DynamicMaxValue
        {
            get => dynamicMaxValue;
            private set { dynamicMaxValue = value; OnPropertyChanged(); }
        }


        // ===========================
        // INotifyPropertyChanged
        // ===========================
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
