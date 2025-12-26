using MonitorApp.Views.Pages.TrafficMonitor.TM_Center;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MonitorApp.Services;
using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;

namespace MonitorApp.Views.Pages.TrafficMonitor
{


    public partial class TM_Header : UserControl
    {
        private readonly MonitorApiClient _api = new();
        private string _currentRange = "5m";
        public TM_Header()
        {
            InitializeComponent();
        }

        // Button "Graph"
        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var parent = Window.GetWindow(this);
            if (parent == null) return;

            var frame = parent.FindName("MainContentFrame") as Frame;
            if (frame != null)
                frame.Content = new TM_Center.Graph();    // <-- chuyển sang Graph
        }

        // Button "Usage"
        private void ToggleButton_Checked_1(object sender, RoutedEventArgs e)
        {
            var parent = Window.GetWindow(this);
            if (parent == null) return;

            var frame = parent.FindName("MainContentFrame") as Frame;
            if (frame != null)
                frame.Content = new TM_Center.Usage();    // <-- chuyển sang Usage
        }


        private static Graph? FindAncestorOrDescendantGraph(DependencyObject start)
        {
            // Leo lên 1 root tương đối (đủ để cover TM_Main)
            var root = start;
            for (int i = 0; i < 10; i++)
            {
                var parent = VisualTreeHelper.GetParent(root);
                if (parent == null) break;
                root = parent;
            }

            return FindDescendant<Graph>(root);
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cb) return;
            var range = (cb.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "5m";

            _currentRange = range;                 // ✅ lưu để export dùng đúng range
            TrafficRangeBus.SetRange(range);
        }
        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var safeRange = _currentRange?.Trim() ?? "5m";
            // 1) chọn nơi lưu trước
            var dlg = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                FileName = $"traffic-report-{DateTime.Now:yyyyMMdd-HHmm}-{safeRange}.pdf"
            };

            if (dlg.ShowDialog() != true) return;

            // 2) gọi API lấy pdf bytes
            var bytes = await _api.GetTrafficExportPdfAsync(_currentRange);
            if (bytes == null || bytes.Length == 0)
            {
                MessageBox.Show("Không lấy được file PDF từ API.", "Export PDF",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3) lưu xuống đúng path đã chọn
            await File.WriteAllBytesAsync(dlg.FileName, bytes);

            MessageBox.Show("Xuất PDF thành công.", "Export PDF",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }





        private static T? FindDescendant<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj == null) return null;

            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T typed) return typed;

                var found = FindDescendant<T>(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}
