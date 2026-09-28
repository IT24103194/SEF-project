using Microsoft.EntityFrameworkCore;

namespace SmartGym.Api.DTOs.Common;

public class PaginationParameters
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

    public int Skip => (PageNumber - 1) * PageSize;
    public int Take => PageSize;

    public PaginationParameters() { }

    public PaginationParameters(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}

public class SortParameters
{
    public string? SortBy { get; set; }
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
    public bool IsDescending => Direction == SortDirection.Descending;

    public SortParameters() { }

    public SortParameters(string? sortBy, SortDirection direction = SortDirection.Ascending)
    {
        SortBy = sortBy;
        Direction = direction;
    }

    public static SortParameters FromString(string? sortBy, bool descending = false)
    {
        return new SortParameters(sortBy, descending ? SortDirection.Descending : SortDirection.Ascending);
    }
}

public class FilterParameters
{
    public string FieldName { get; set; } = string.Empty;
    public string Operator { get; set; } = "eq"; // eq, neq, gt, gte, lt, lte, contains, in
    public string Value { get; set; } = string.Empty;

    public FilterParameters() { }

    public FilterParameters(string fieldName, string value, string op = "eq")
    {
        FieldName = fieldName;
        Value = value;
        Operator = op;
    }
}

public class PagedRequest : PaginationParameters
{
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
}

public enum SortDirection
{
    Ascending = 1,
    Descending = 2
}

// Backward-compatibility aliases
public class SortParam : SortParameters { }
public class FilterParam : FilterParameters { }

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
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
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

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }
}
