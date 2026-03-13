namespace B2BSpareParts.Application.Common;

public class PageRequest
{
    private const int MaxPageSize = 100;

    public int PageNumber { get; set; } = 1;

    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : Math.Max(1, value);
    }

    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
}
