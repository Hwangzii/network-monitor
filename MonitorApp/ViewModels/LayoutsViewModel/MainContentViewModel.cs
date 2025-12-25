using System;
using System.Windows.Controls;
using MonitorApp.Views.Pages.TrafficMonitor;
using MonitorApp.Views.Pages;

namespace MonitorApp.ViewModels.LayoutsViewModel
{
    public class MainContentViewModel : BaseViewModel
    {
        private UserControl _currentPage;
        public UserControl CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }

        public MainContentViewModel()
        {
            // trang mặc định
            CurrentPage = new TM_Main();

            // đăng ký event Global từ HeaderViewModel
            HeaderViewModel.GlobalPageChanged += OnPageChanged;
        }

        private void OnPageChanged(string pageName)
        {
            switch (pageName)
            {
                case "Traffic":
                    CurrentPage = new TM_Main();
                    break;

                case "Protect":
                    CurrentPage = new GrassWireProtect();
                    break;

                case "Speed":
                    CurrentPage = new SpeedTest();
                    break;

                case "Scanner":
                    CurrentPage = new NetworkScanner();
                    break;
            }
        }
    }
}
