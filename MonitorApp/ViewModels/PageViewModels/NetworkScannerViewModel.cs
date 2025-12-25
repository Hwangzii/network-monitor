using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MonitorApp.Models;
using System.Windows.Media;
using MonitorApp.Services;

namespace MonitorApp.ViewModels.PageViewModels
{
    public class NetworkDevice
    {
        public bool IsOnline { get; set; }
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string System { get; set; }
        public string Ports { get; set; }
        public string MacAddress { get; set; }
        public string LastSeen { get; set; }
        public string FirstSeen { get; set; }
    }

    public class NetworkScannerViewModel : INotifyPropertyChanged
    {
        private readonly MonitorApiClient _api = new();

        public ObservableCollection<NetworkDevice> Devices { get; } =
            new ObservableCollection<NetworkDevice>();

        private string _selectedDeviceName = "DESKTOP của đạt";
        public string SelectedDeviceName
        {
            get => _selectedDeviceName;
            set { _selectedDeviceName = value; OnPropertyChanged(); }
        }

        private string _lastSeenSummary = "11 Nov, 2025";
        public string LastSeenSummary
        {
            get => _lastSeenSummary;
            set { _lastSeenSummary = value; OnPropertyChanged(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        private bool _isAutoScanEnabled = true;
        public bool IsAutoScanEnabled
        {
            get => _isAutoScanEnabled;
            set { _isAutoScanEnabled = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // gọi khi UserControl Loaded
        public async Task InitializeAsync()
        {
            await Task.WhenAll(LoadWifiAsync(), LoadDevicesAsync());
        }

        private async Task LoadWifiAsync()
        {
            try
            {
                var wifi = await _api.GetWifiInfoAsync();
                if (wifi != null && wifi.IsConnected && !string.IsNullOrWhiteSpace(wifi.Ssid))
                {
                    // "bao" sẽ hiện ở chỗ DESKTOP của đạt
                    SelectedDeviceName = wifi.Ssid;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadWifiAsync error: " + ex.Message);
            }
        }


        public async Task LoadDevicesAsync()
        {
            try
            {
                var apiDevices = await _api.GetScannerDevicesAsync();
                if (apiDevices == null) return;

                var list = await Task.Run(() =>
                    apiDevices.Select(d => new NetworkDevice
                    {
                        IsOnline = d.IsOnline,
                        Name = d.Name,
                        IpAddress = d.Ip,
                        Location = d.Location,
                        Description = d.Description,
                        System = d.System,
                        Ports = d.Ports,
                        MacAddress = d.Mac_Address,
                        LastSeen = d.Last_Seen,
                        FirstSeen = d.First_Seen
                    }).ToList()
                );


                Devices.Clear();
                foreach (var item in list) Devices.Add(item);

                var first = Devices.FirstOrDefault();
                if (first != null && !string.IsNullOrWhiteSpace(first.LastSeen))
                    LastSeenSummary = first.LastSeen;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadDevicesAsync error: " + ex.Message);
            }
        }

        private void AddRange<T>(ObservableCollection<T> collection, IEnumerable<T> items)
        {
            if (collection == null || items == null) return;

            foreach (var item in items)
                collection.Add(item);
        }

    }
}
