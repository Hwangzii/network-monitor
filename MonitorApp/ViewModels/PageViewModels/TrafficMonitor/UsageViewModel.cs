using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MonitorApp.Helpers;
using System.Threading;
using MonitorApp.Models;
using MonitorApp.Services;
using System.Diagnostics;


namespace MonitorApp.ViewModels.PageViewModels.TrafficMonitor
{
    public class UsageViewModel : INotifyPropertyChanged
    {
        private readonly MonitorApiClient _api = new();
        private readonly DispatcherTimer _timer;
        private bool _isLoading;

        public ObservableCollection<UsageAppItem> AppsData { get; } = new();
        public ObservableCollection<UsageHostItem> HostsData { get; } = new();
        public ObservableCollection<UsageTrafficTypeItem> TrafficTypeData { get; } = new();
        public ObservableCollection<UsageCountryItem> CountriesData { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public UsageViewModel()
        {
            Debug.WriteLine("🔥 UsageViewModel CREATED");

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
            _timer.Tick += async (_, __) => await LoadAsync();
            _timer.Start();

            _ = LoadAsync();
        }

        private readonly SemaphoreSlim _loadGate = new(1, 1);
        private CancellationTokenSource? _cts;

        public async Task LoadAsync()
        {
            if (!await _loadGate.WaitAsync(0)) return;
            _cts?.Cancel();
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));

            try
            {
                Debug.WriteLine("⏳ LoadAsync START");

                var usageTask = _api.GetTrafficUsageSummaryAsync(_cts.Token);

                var usage = await usageTask;

                if (usage == null)
                {
                    Debug.WriteLine("⚠️ usage-summary NULL -> clear UI");
                    AppsData.Clear();
                    HostsData.Clear();
                    TrafficTypeData.Clear();
                    CountriesData.Clear();
                    return;
                }

                FillApps(usage);
                FillHosts(usage);
                FillTrafficTypes(usage);
                FillCountries(usage);

                Debug.WriteLine("✅ LoadAsync DONE");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("⚠️ LoadAsync CANCELED/TIMEOUT");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("💥 LoadAsync ERROR: " + ex);
            }
            finally
            {
                _loadGate.Release();
            }
        }

        private void FillApps(TrafficUsageSummaryResponse resp)
        {
            AppsData.Clear();

            Debug.WriteLine($"📦 FillApps | Count={resp.Apps?.Count}");

            if (resp.Apps == null || resp.Apps.Count == 0)
            {
                Debug.WriteLine("⚠️ Apps EMPTY");
                return;
            }

            long max = Math.Max(1, resp.Apps.Max(x => x.UsageBytes));

            foreach (var a in resp.Apps.OrderByDescending(x => x.UsageBytes))
            {
                Debug.WriteLine($"➡️ App: {a.Name} {a.UsageBytes}");

                AppsData.Add(new UsageAppItem
                {
                    Name = a.Name ?? "",
                    Size = a.Usage ?? "",
                    Icon = ImageHelper.FromBase64DataUri(a.AppIcon),
                    Flag = ImageHelper.FromUrl(a.CountryFlagUrl),
                    Progress = (a.UsageBytes * 100.0) / max
                });
            }

            Debug.WriteLine($"✅ AppsData UI Count = {AppsData.Count}");
        }

        private void FillHosts(TrafficUsageSummaryResponse resp)
        {
            HostsData.Clear();

            var hosts = resp.Hosts ?? new();
            long max = hosts.Count == 0 ? 1 : Math.Max(1, hosts.Max(x => x.UsageBytes));

            foreach (var h in hosts.OrderByDescending(x => x.UsageBytes))
            {
                HostsData.Add(new UsageHostItem
                {
                    Host = h.Hostname ?? "",
                    Size = h.Usage ?? "",
                    Flag = ImageHelper.FromUrl(h.CountryFlagUrl),
                    Icon = ImageHelper.FromBase64DataUri(h.AppOwnerIcon),
                    Progress = (h.UsageBytes * 100.0) / max
                });
            }
        }

        private void FillTrafficTypes(TrafficUsageSummaryResponse resp)
        {
            TrafficTypeData.Clear();

            var types = resp.TrafficTypes ?? new();
            foreach (var t in types.OrderByDescending(x => x.Percentage))
            {
                TrafficTypeData.Add(new UsageTrafficTypeItem
                {
                    Type = t.Type ?? "",
                    Size = t.Usage ?? "",
                    Progress = Math.Max(0, Math.Min(100, t.Percentage))
                });
            }
        }


        private void FillCountries(TrafficUsageSummaryResponse resp)
        {
            CountriesData.Clear();

            var countries = resp.Countries ?? new();
            long max = countries.Count == 0 ? 1 : Math.Max(1, countries.Max(x => x.UsageBytes));

            foreach (var c in countries.OrderByDescending(x => x.UsageBytes))
            {
                CountriesData.Add(new UsageCountryItem
                {
                    Country = c.Country ?? "",                      
                    Size = c.Usage ?? "",
                    Flag = ImageHelper.FromUrl(c.CountryFlagUrl),   
                    Progress = (c.UsageBytes * 100.0) / max
                });
            }
        }



        protected void OnPropertyChanged([CallerMemberName] string name = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
