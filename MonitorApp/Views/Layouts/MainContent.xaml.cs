using MonitorApp.ViewModels.LayoutsViewModel;
using MonitorApp.Views.Pages;
using MonitorApp.Views.Pages.TrafficMonitor;
using System.Windows.Controls;

namespace MonitorApp.Views.Layouts
{
    public partial class MainContent : UserControl
    {
        public MainContent()
        {
            InitializeComponent();
            DataContext = new MainContentViewModel();  // <-- MVVM chuẩn
        }
    }
}
