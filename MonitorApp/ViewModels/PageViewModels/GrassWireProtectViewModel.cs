using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using MonitorApp.Models;
using MonitorApp.Services;

namespace MonitorApp.ViewModels.PageViewModels
{
    public class GrassWireProtectViewModel : INotifyPropertyChanged
    {
        private readonly MonitorApiClient _apiClient = new();
        private readonly DispatcherTimer _refreshTimer;

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<GrassWireProtectApp> _appList;
        public ObservableCollection<GrassWireProtectApp> AppList
        {
            get => _appList;
            set { _appList = value; OnPropertyChanged(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterApps();
            }
        }

        private ObservableCollection<GrassWireProtectApp> _filteredAppList;
        public ObservableCollection<GrassWireProtectApp> FilteredAppList
        {
            get => _filteredAppList;
            set { _filteredAppList = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        private int _currentPage = 1;
        private const int PageSize = 20;
        private int _totalPages = 1;
        private bool _isLoadingMore = false;

        public GrassWireProtectViewModel()
        {
            AppList = new ObservableCollection<GrassWireProtectApp>();
            FilteredAppList = new ObservableCollection<GrassWireProtectApp>();

            // Tự động refresh mỗi 5 giây
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _refreshTimer.Tick += async (s, e) =>
            {
                // Option 1: chỉ refresh lại trang 1
                _currentPage = 1;
                await LoadAppsAsync(1);
            };

            _refreshTimer.Start();

            // Load dữ liệu lần đầu
            _ = LoadAppsAsync(1);
        }

        /// <summary>
        /// Load apps từ API với pagination
        /// </summary>
        public async Task LoadAppsAsync(int page = 1)
        {
            if (_isLoadingMore || (page > 1 && page > _totalPages))
                return;

            try
            {
                if (page == 1)
                {
                    IsLoading = true;
                    AppList.Clear();
                }

                _isLoadingMore = true;
                ErrorMessage = string.Empty;

                var response = await _apiClient.GetFirewallAppsAsync(page, PageSize, "active", true);

                if (response?.Data != null)
                {
                    // Page 1: clear và load mới, page > 1: append
                    if (page == 1)
                    {
                        AppList.Clear();
                    }

                    foreach (var app in response.Data)
                    {
                        AppList.Add(app);
                    }

                    // Lưu total pages
                    if (response.Pagination != null)
                    {
                        _totalPages = response.Pagination.TotalPages;
                    }

                    _currentPage = page;
                    FilterApps();
                }
                else
                {
                    if (page == 1)
                        ErrorMessage = "Không thể tải dữ liệu từ API";
                }
            }
            catch (Exception ex)
            {
                if (_currentPage == 1)
                    ErrorMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                _isLoadingMore = false;
            }
        }

        /// <summary>
        /// Load thêm trang tiếp theo khi cuộn xuống
        /// </summary>
        public async Task LoadMoreAsync()
        {
            if (_currentPage < _totalPages)
            {
                await LoadAppsAsync(_currentPage + 1);
            }

            System.Diagnostics.Debug.WriteLine($"LoadMoreAsync: current={_currentPage}, total={_totalPages}");
            if (_currentPage < _totalPages)
                await LoadAppsAsync(_currentPage + 1);
        }

        private void FilterApps()
        {
            FilteredAppList.Clear();

            var filtered = AppList;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = new ObservableCollection<GrassWireProtectApp>(
                    AppList.Where(a =>
                        a.AppName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        a.AppId.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                    ).ToList()
                );
            }

            foreach (var app in filtered)
            {
                FilteredAppList.Add(app);
            }
        }

        public void Dispose()
        {
            _refreshTimer?.Stop();
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
