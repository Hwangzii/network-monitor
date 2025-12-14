namespace NetworkMonitor.Api.DTOs;

public class PagingInfo
{
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int Limit { get; set; }
}

// Đối tượng phản hồi chung có phân trang
public class PagedResponseDto<T> where T : class
{
    public PagingInfo Pagination { get; set; } = new();
    public List<T> Data { get; set; } = new();
}