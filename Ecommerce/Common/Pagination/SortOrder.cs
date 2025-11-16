namespace Ecommerce.Common.Pagination;

public enum SortOrder
{
    Ascending,
    Descending
}

internal static class SortOrderExtensions
{
    public static string ToSql(this SortOrder sortOrder)
    {
        return sortOrder switch
        {
            SortOrder.Ascending => "ASC",
            SortOrder.Descending => "DESC",
            _ => throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder, null)
        };
    }
}