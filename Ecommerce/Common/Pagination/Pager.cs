namespace Ecommerce.Common.Pagination;

public class Pager
{

    private int _pageNumber = 1;
    
    private int _pageSize = 10;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        
        set => _pageSize = value < 1 ? 10 : value;
    }
    
    public int Offset => (PageNumber - 1) * PageSize;

    public Pager()
    {
        
    }
    
}