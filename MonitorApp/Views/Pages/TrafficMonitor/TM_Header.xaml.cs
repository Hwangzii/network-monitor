using MonitorApp.Views.Pages.TrafficMonitor.TM_Center;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MonitorApp.Services;

namespace MonitorApp.Views.Pages.TrafficMonitor
{
    public partial class TM_Header : UserControl
    {
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
            var range = (cb.SelectedItem as ComboBoxItem)?.Content?.ToString();
            TrafficRangeBus.SetRange(range ?? "5m");
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
