using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MonitorApp.ViewModels.PageViewModels.TrafficMonitor
{
    public class TM_HeaderViewModel : INotifyPropertyChanged
    {
        // ===========================
        // 1) Time selection
        // ===========================
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

        // ===========================
        // 2) View mode (Graph or Grid)
        // ===========================
        private bool _isGraphSelected = true;
        public bool IsGraphSelected
        {
            get => _isGraphSelected;
            set
            {
                if (_isGraphSelected != value)
                {
                    _isGraphSelected = value;
                    OnPropertyChanged();

                    // Đồng bộ hóa với Usage
                    if (value)
                    {
                        IsUsageSelected = false;
                        OnViewModeChanged?.Invoke("Graph");
                    }
                }
            }
        }

        private bool _isUsageSelected;
        public bool IsUsageSelected
        {
            get => _isUsageSelected;
            set
            {
                if (_isUsageSelected != value)
                {
                    _isUsageSelected = value;
                    OnPropertyChanged();

                    // Đồng bộ hóa với Graph
                    if (value)
                    {
                        IsGraphSelected = false;
                        OnViewModeChanged?.Invoke("Grid");
                    }
                }
            }
        }

        // ===========================
        // 3) Commands
        //============================
        public ICommand ShowGraphCommand { get; }
        public ICommand ShowUsageCommand { get; }
        public ICommand? TimeChangedCommand { get; set; }

        // Notify external VM (like TM_Main)
        public Action<string>? OnViewModeChanged { get; set; }

        // ===========================
        // Constructor
        // ===========================
        public TM_HeaderViewModel()
        {
            // mặc định ở view Graph
            IsGraphSelected = true;
            IsUsageSelected = false;

            ShowGraphCommand = new RelayCommand(_ => IsGraphSelected = true);
            ShowUsageCommand = new RelayCommand(_ => IsUsageSelected = true);
        }

        // ===========================
        // INotifyPropertyChanged
        // ===========================
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // RelayCommand
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged;
    }
}
