using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        private const int MaxDataPoints = 60;
        private const double SampleIntervalSeconds = 1.0;
        private const double LabelIntervalSeconds = 4.0;
        private const double PointWidthPx = 30.0;  // ✅ Width per data point in pixels
        private const double LabelWidth = 150.0;

        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _apiTimer;
        private readonly MonitorApiClient _api = new();
        
        // ✅ Dùng Stopwatch để tính thời gian chính xác
        private readonly Stopwatch _labelStopwatch = Stopwatch.StartNew();
        
        // ✅ Flag để kiểm soát cập nhật nhãn chỉ 1 lần/4s
        private int _lastLabelUpdateIndex = -1;
        
        private DateTime _lastLabelDateTime;

        // ✅ Pre-buffered data to avoid gaps during API delays
        private double _lastDownloadSpeed = 0;
        private double _lastUploadSpeed = 0;
        private DateTime _lastDataAddTime = DateTime.Now;

        // ✅ Dynamic chart width - expands as data grows
        private double _chartWidth = 800; // Initial width
        public double ChartWidth
        {
            get => _chartWidth;
            private set { _chartWidth = value; OnPropertyChanged(); }
        }

        // ✅ Horizontal scroll offset to keep right edge visible
        private double _horizontalScrollOffset = 0;
        public double HorizontalScrollOffset
        {
            get => _horizontalScrollOffset;
            private set { _horizontalScrollOffset = value; OnPropertyChanged(); }
        }

        private ObservableCollection<double> downloadData;
        private ObservableCollection<double> uploadData;
        private ObservableCollection<string> timeLabels;
        private readonly List<string> allTimeLabels = new();
        private int _totalDataPointsEverAdded = 0;  // Track total points for infinite scrolling

        private double downloadSpeed;
        private double uploadSpeed;
        private double smoothScrollOffset;
        private DateTime lastApiTime;
        private int lastDataCount;

        private double dynamicMaxValue = 100.0;
        public string MaxLabel => FormatDataRate(DynamicMaxValue);

        // thêm field này
        private DateTime lastLabelTime;

        // ===========================
        // Ctor
        // ===========================
        public GraphViewModel()
        {
            InitializeData();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();

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
            private set
            {
                dynamicMaxValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MaxLabel));
            }
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
            _totalDataPointsEverAdded = MaxDataPoints;

            // Khởi tạo timeline: tạo 25 nhãn ban đầu (bắt đầu từ -100s)
            var now = DateTime.Now;
            _lastLabelDateTime = now.AddSeconds(-25 * LabelIntervalSeconds);
            lastLabelTime = _lastLabelDateTime;

            for (int i = 0; i < 25; i++)
            {
                var t = _lastLabelDateTime.AddSeconds(i * LabelIntervalSeconds);
                allTimeLabels.Add(t.ToString("h:mm:ss tt"));
            }

            _lastLabelDateTime = _lastLabelDateTime.AddSeconds(24 * LabelIntervalSeconds);
            lastApiTime = now;
            lastDataCount = MaxDataPoints;
            _lastDataAddTime = now;

            // ✅ Initial chart width
            UpdateChartWidth();
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
                return;
            }

            if (summary == null)
                return;

            double newDownloadSpeed = ParseSpeed(summary.DownloadSpeed);
            double newUploadSpeed = ParseSpeed(summary.UploadSpeed);

            DownloadSpeed = newDownloadSpeed;
            UploadSpeed = newUploadSpeed;

            // ✅ Add real data from API
            DownloadData.Add(newDownloadSpeed);
            UploadData.Add(newUploadSpeed);
            _totalDataPointsEverAdded++;

            // Keep only recent MaxDataPoints visible, but track total for infinite scroll
            if (DownloadData.Count > MaxDataPoints)
            {
                DownloadData.RemoveAt(0);
                UploadData.RemoveAt(0);
            }

            // ✅ Store for pre-buffering next points
            _lastDownloadSpeed = newDownloadSpeed;
            _lastUploadSpeed = newUploadSpeed;
            _lastDataAddTime = DateTime.Now;

            lastApiTime = DateTime.Now;
            lastDataCount = DownloadData.Count;

            // ✅ Update chart width to accommodate new data
            UpdateChartWidth();

            OnPropertyChanged(nameof(DownloadData));
            OnPropertyChanged(nameof(UploadData));
        }

        // ===========================
        // Tick – SYNCHRONIZED với Stopwatch (KHÔNG GIẬT)
        // ===========================
        private void OnTimerTick(object? sender, EventArgs e)
        {
            // ✅ TIMELINE: Tịnh tiến mượt mà - dựa trên thời gian elapsed
            double elapsedSeconds = _labelStopwatch.Elapsed.TotalSeconds;
            
            // Progress từ 0 → 1 trong mỗi khoảng 4 giây
            double progress = (elapsedSeconds % LabelIntervalSeconds) / LabelIntervalSeconds;
            
            // ✅ Scroll offset: từ 0 → -150px (phải sang trái - đẩy dữ liệu cũ sang trái)
            // progress = 0: offset = 0 (timeline ở vị trí ban đầu)
            // progress = 1: offset = -150 (timeline đẩy sang trái để nhãn mới xuất hiện)
            double scrollOffset = -progress * LabelWidth;
            SmoothScrollOffset = scrollOffset;

            // ✅ CHỈ UPDATE NHÃN 1 LẦN/4S (dùng index, không dùng time range)
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

            // AUTO-SCALE Y
            double maxY = 0;

            if (DownloadData != null)
            {
                foreach (var v in DownloadData)
                    if (v > maxY) maxY = v;
            }

            if (UploadData != null)
            {
                foreach (var v in UploadData)
                    if (v > maxY) maxY = v;
            }

            double targetMax = Math.Max(20, maxY * 1.2);

            const double lerpSpeed = 0.15;
            if (double.IsNaN(DynamicMaxValue) || DynamicMaxValue <= 0)
                DynamicMaxValue = targetMax;
            else
                DynamicMaxValue = DynamicMaxValue + (targetMax - DynamicMaxValue) * lerpSpeed;
        }

        // ✅ Update chart width based on data points
        private void UpdateChartWidth()
        {
            ChartWidth = Math.Max(800, DownloadData.Count * PointWidthPx);
        }

        private void UpdateVisibleTimeLabels()
        {
            TimeLabels.Clear();

            int visibleLabelCount = (int)(DownloadData.Count / (LabelIntervalSeconds / SampleIntervalSeconds));
            if (visibleLabelCount < 5) visibleLabelCount = 5;

            int startIndex = Math.Max(0, allTimeLabels.Count - visibleLabelCount);
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

            // ✅ Mỗi điểm cách nhau PointWidthPx pixel
            double max = DynamicMaxValue;

            // Điểm bắt đầu ở dưới cùng (cho Polygon/area)
            points.Add(new Point(0, height));

            // Điểm dữ liệu
            for (int i = 0; i < data.Count; i++)
            {
                // X position = điểm thứ i * chiều rộng mỗi điểm
                double x = i * PointWidthPx;
                double y = height - (data[i] / max * height);
                points.Add(new Point(x, y));
            }

            // Điểm kết thúc ở dưới cùng
            double endX = (data.Count - 1) * PointWidthPx;
            points.Add(new Point(endX, height));

            return points;
        }

        public void StopMonitoring()
        {
            _timer?.Stop();
            _apiTimer?.Stop();
            _labelStopwatch?.Stop();
        }

        // ===========================
        // Helpers
        // ===========================
        private double ParseSpeed(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            text = text.Replace("/s", "", StringComparison.OrdinalIgnoreCase).Trim();

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
                return value * 1024 * 1024;
            if (upper.Contains("MB"))
                return value * 1024;
            if (upper.Contains("KB"))
                return value;
            if (upper.Contains("B"))
                return value / 1024;

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
