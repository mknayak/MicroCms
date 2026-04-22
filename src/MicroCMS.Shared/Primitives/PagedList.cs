namespace MicroCMS.Shared.Primitives;

/// <summary>
/// Immutable paginated result container returned by query handlers.
/// </summary>
#pragma warning disable CA1000 // Do not declare static members on generic types - acceptable for factory pattern
public sealed class PagedList<T>
#pragma warning restore CA1000
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    private PagedList(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static PagedList<T> Create(IEnumerable<T> source, int page, int pageSize, int totalCount)
        => new(source.ToList().AsReadOnly(), page, pageSize, totalCount);
}
