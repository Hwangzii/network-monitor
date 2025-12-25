using System;

namespace MonitorApp.Services
{
    public static class TrafficRangeBus
    {
        private static string _range = "5m";
        public static string CurrentRange => _range;

        public static event Action<string>? RangeChanged;

        public static void SetRange(string range)
        {
            range = (range ?? "").Trim().ToLowerInvariant();
            if (range == "24 hours") range = "24h";
            if (range != "5m" && range != "3h" && range != "24h") range = "5m";

            if (_range == range) return;
            _range = range;
            RangeChanged?.Invoke(_range);
        }
    }
}
