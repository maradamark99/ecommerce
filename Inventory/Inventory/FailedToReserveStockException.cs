using Inventory.Contract;

namespace Inventory;

public class FailedToReserveStockException : Exception
{
    public FailedToReserveStockException() : base()
    {
    }
    
    public FailedToReserveStockException(string message) : base(message)
    {
    }   
    
}