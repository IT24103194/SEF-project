namespace SmartGym.Api.DTOs.Common;

public class PagedRequest
{
    private const int MaxPageSizeLimit = 100;
    private int _pageNumber = 1;
    private int _pageSize = 20;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSizeLimit ? MaxPageSizeLimit : (value < 1 ? 20 : value);
    }

    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
}

public enum SortDirection
{
    Ascending = 1,
    Descending = 2
}

public class SortParam
{
    public string SortBy { get; set; } = string.Empty;
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
}

public class FilterParam
{
    public string FieldName { get; set; } = string.Empty;
    public string Operator { get; set; } = "eq"; // eq, neq, gt, gte, lt, lte, contains, in
    public string Value { get; set; } = string.Empty;
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }

    public PagedResult() { }

    public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items.ToList().AsReadOnly();
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize > 0 ? pageSize : 10;
        TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
        HasPreviousPage = PageNumber > 1;
        HasNextPage = PageNumber < TotalPages;
    }

    public static PagedResult<T> Create(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
    }
}
