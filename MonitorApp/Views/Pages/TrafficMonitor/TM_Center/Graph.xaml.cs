using System;
using System.Windows;
using System.Windows.Controls;
using MonitorApp.ViewModels.PageViewModels.TrafficMonitor;

namespace MonitorApp.Views.Pages.TrafficMonitor.TM_Center
{
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    public partial class Graph : UserControl
    {
        private GraphViewModel _viewModel;

        public Graph()
        {
            InitializeComponent();
            Loaded += Graph_Loaded;
        }

        private void Graph_Loaded(object sender, RoutedEventArgs e)
        {
            // ✅ Get ViewModel
            _viewModel = DataContext as GraphViewModel;
            
            if (_viewModel != null)
            {
                // ✅ Chart auto-scroll to right when width increases
                ChartScroller.ScrollChanged += (s, args) =>
                {
                    if (args.ExtentWidthChange > 0)
                    {
                        ChartScroller.ScrollToRightEnd();
                    }
                };

                // ✅ Scroll to right immediately
                ChartScroller.Dispatcher.BeginInvoke(() =>
                {
                    ChartScroller.ScrollToRightEnd();
                });
            }
        }
    }
}
