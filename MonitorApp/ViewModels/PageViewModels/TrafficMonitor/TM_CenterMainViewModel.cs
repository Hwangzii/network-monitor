using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MonitorApp.ViewModels.PageViewModels.TrafficMonitor
{
    public class TM_CenterMainViewModel : INotifyPropertyChanged
    {
        private object _currentPage;

        private readonly object graphPage;
        private readonly object usagePage;

        // ViewModels
        private readonly GraphViewModel graphVM = new GraphViewModel();
        private readonly UsageViewModel usageVM = new UsageViewModel();

        public TM_CenterMainViewModel()
        {
            graphPage = new MonitorApp.Views.Pages.TrafficMonitor.TM_Center.Graph
            {
                DataContext = graphVM
            };

            usagePage = new MonitorApp.Views.Pages.TrafficMonitor.TM_Center.Usage
            {
                DataContext = usageVM
            };

            CurrentPage = graphPage;

            ShowGraphCommand = new RelayCommand(_ => ShowGraph());
            ShowUsageCommand = new RelayCommand(_ => ShowUsage());
        }

        public object CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGraphSelected));
                OnPropertyChanged(nameof(IsUsageSelected));
            }
        }


        public ICommand ShowGraphCommand { get; }
        public ICommand ShowUsageCommand { get; }
        public bool IsGraphSelected => CurrentPage == graphPage;
        public bool IsUsageSelected => CurrentPage == usagePage;


        private void ShowGraph()
        {
            CurrentPage = graphPage;
            OnPropertyChanged(nameof(IsGraphSelected));
            OnPropertyChanged(nameof(IsUsageSelected));
        }
        private void ShowUsage()
        {
            CurrentPage = usagePage;
            OnPropertyChanged(nameof(IsGraphSelected));
            OnPropertyChanged(nameof(IsUsageSelected));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public class RelayCommand : ICommand
        {
            private readonly Action<object> execute;

            public RelayCommand(Action<object> exec)
            {
                execute = exec;
            }

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter) => execute(parameter);

            public event EventHandler CanExecuteChanged;
        }


    }
}
