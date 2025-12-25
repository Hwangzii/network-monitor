using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MonitorApp.Models
{
    /// <summary>
    /// DTO cho ứng dụng trong Firewall/Protect
    /// </summary>
    public class GrassWireProtectApp
    {
        public string AppId { get; set; }
        public string AppName { get; set; }
        public string InConnections { get; set; }  // "Allowed" / "Blocked"
        public string OutConnections { get; set; } // "Allowed" / "Blocked"
        public string Version { get; set; }
        public string Hosts { get; set; }
        public string VirusTotal { get; set; }
        public string DownloadSpeed { get; set; }
        public string UploadSpeed { get; set; }
        public string IconBase64 { get; set; }
    }

    /// <summary>
    /// Response từ API /firewall/apps
    /// </summary>
    public class GrassWireProtectResponse
    {
        public List<GrassWireProtectApp> Data { get; set; }
        public PaginationInfo Pagination { get; set; }
    }

    public class PaginationInfo
    {
        [JsonPropertyName("totalRecords")]
        public int Total { get; set; }

        [JsonPropertyName("currentPage")]
        public int Page { get; set; }

        [JsonPropertyName("limit")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }
    }
}
