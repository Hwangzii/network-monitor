using System.ComponentModel;
using System.Runtime.CompilerServices;
using MonitorApp.ViewModels.Layouts;
using MonitorApp.Views.Pages;
using MonitorApp.Views.Pages.TrafficMonitor;



namespace MonitorApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object? _currentPage;

        public HeaderViewModel HeaderVM { get; }

        public object? CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            HeaderVM = new HeaderViewModel();
            HeaderVM.PageChanged += OnPageChanged;

            // Trang mặc định
            CurrentPage = new TM_Main();
        }

        private void OnPageChanged(string page)
        {
            switch (page)
            {
                case "Traffic":
                    CurrentPage = new TM_Main();
                    break;
                case "GlassWire":
                    CurrentPage = new GrassWireProtect();
                    break;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
