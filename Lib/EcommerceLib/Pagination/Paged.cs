namespace EcommerceLib.Pagination;

public class Paged<T>
{
    public IList<T> Items { get; set; }
    
    public int Page { get; init; }
    
    public int PageSize { get; init; }
    
    public long TotalItems { get; init;  }
    
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    
    public bool HasPreviousPage => Page > 1;
    
    public bool HasNextPage => Page < TotalPages;
    
    public static Paged<T> Of(IEnumerable<T> items, int page, int pageSize, long totalItems)
    {
        return new Paged<T>
        {
            Items = items.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }
    
    public static Paged<T> Empty(int page, int pageSize)
    {
        return new Paged<T>
        {
            Items = [],
            Page = page,
            PageSize = pageSize,
            TotalItems = 0
        };
    }
    
    public Paged<R> Select<R>(Func<T, R> mapper)
    {
        return new Paged<R>
        {
            Items = Items.Select(mapper).ToList(),
            Page = Page,
            PageSize = PageSize,
            TotalItems = TotalItems
        };
    }
}
