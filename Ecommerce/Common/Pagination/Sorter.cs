namespace Ecommerce.Common.Pagination;

public class Sorter
{
    public string SortBy { get; set; } = "Id";

    public SortOrder SortOrder { get; set; } = SortOrder.Ascending;

    public Sorter()
    {
        
    }
}

