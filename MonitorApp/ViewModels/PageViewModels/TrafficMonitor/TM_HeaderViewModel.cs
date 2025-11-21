// ViewModels/PageViewModels/TrafficMonitor/TM_HeaderViewModel.cs
using MonitorApp.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MonitorApp.ViewModels.PageViewModels.TrafficMonitor
{
    public class TM_HeaderViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public TrafficMonitorViewModel? ParentViewModel { get; set; }

        // Dùng string thay vì enum → không lỗi
        public string SelectedRange
        {
            get => ParentViewModel?.SelectedRange ?? "5m";
            set
            {
                if (ParentViewModel != null && ParentViewModel.SelectedRange != value)
                    ParentViewModel.SelectedRange = value;
            }
        }

        public ICommand ShowGraphCommand { get; }
        public ICommand ShowUsageCommand { get; }

        public TM_HeaderViewModel()
        {
            ShowGraphCommand = new RelayCommand(_ => { });
            ShowUsageCommand = new RelayCommand(_ => { });
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}