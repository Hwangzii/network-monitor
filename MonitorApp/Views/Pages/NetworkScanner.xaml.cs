using System.Windows;
using System.Windows.Controls;
using MonitorApp.ViewModels.PageViewModels;

namespace MonitorApp.Views.Pages
{
    public partial class NetworkScanner : UserControl
    {
        public NetworkScanner()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is NetworkScannerViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}
