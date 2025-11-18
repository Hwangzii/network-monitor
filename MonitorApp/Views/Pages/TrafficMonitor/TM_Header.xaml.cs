using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

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

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
