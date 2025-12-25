using System.Windows;
using System.Windows.Controls;
using MonitorApp.ViewModels.PageViewModels.TrafficMonitor;

namespace MonitorApp.Views.Pages.TrafficMonitor.TM_Center
{
    public partial class Graph : UserControl
    {
        private GraphViewModel? _viewModel;

        public Graph()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                _viewModel ??= DataContext as GraphViewModel;

                // set viewport width lần đầu
                _viewModel?.SetViewportWidth(ActualWidth);
            };

            SizeChanged += (_, __) =>
            {
                (_viewModel ??= DataContext as GraphViewModel)
                    ?.SetViewportWidth(ActualWidth);
            };
        }

        public void ChangeRange(string range)
        {
            (_viewModel ??= DataContext as GraphViewModel)?.ChangeRange(range);
        }
    }
}
