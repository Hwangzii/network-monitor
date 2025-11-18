using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MonitorApp.ViewModels
{
    public class TM_HeaderViewModel : INotifyPropertyChanged
    {
        private string _selectedTime = "5 Minutes";
        public string SelectedTime
        {
            get => _selectedTime;
            set
            {
                if (_selectedTime != value)
                {
                    _selectedTime = value;
                    OnPropertyChanged();
                    TimeChangedCommand?.Execute(value);
                }
            }
        }

        private bool _isActivityView = true;
        public bool IsActivityView
        {
            get => _isActivityView;
            set
            {
                if (_isActivityView != value)
                {
                    _isActivityView = value;
                    OnPropertyChanged();
                    OnViewModeChanged?.Invoke(_isActivityView ? "Activity" : "Grid");
                }
            }
        }

        // Event để MainViewModel hoặc TM_Main nhận sự thay đổi
        public Action<string>? OnViewModeChanged { get; set; }

        // Optional: RelayCommand nếu muốn
        public ICommand? TimeChangedCommand { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
