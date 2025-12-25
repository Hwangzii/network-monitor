using System.Windows;
using System.Windows.Controls;
using MonitorApp.ViewModels.PageViewModels.TrafficMonitor;

namespace MonitorApp.Views.Pages.TrafficMonitor
{
    public partial class TM_Footer : UserControl
    {
        private TM_FooterViewModel? _vm;

        public TM_Footer()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                _vm ??= DataContext as TM_FooterViewModel;
                _vm?.SetViewportWidth(ActualWidth);
            };

            SizeChanged += (_, __) =>
            {
                (_vm ??= DataContext as TM_FooterViewModel)
                    ?.SetViewportWidth(ActualWidth);
            };
        }
    }
}
