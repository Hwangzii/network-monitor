using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorApp.ViewModels.PageViewModels
{
    public class NetworkDevice
    {
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string System { get; set; }
        public string MacAddress { get; set; }
        public string LastSeen { get; set; }
        public string FirstSeen { get; set; }
    }

    public class NetworkScannerViewModel
    {
        public ObservableCollection<NetworkDevice> Devices { get; } =
            new ObservableCollection<NetworkDevice>();

        public string SelectedDeviceName { get; set; } = "DESKTOP của đạt";
        public string LastSeenSummary { get; set; } = "11 Nov, 2025";
        public string SearchText { get; set; }
        public bool IsAutoScanEnabled { get; set; } = true;
    }

}
