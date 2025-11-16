namespace OrderManagement.Inventory;

public interface IInventoryClient
{
    Task<HttpResponseMessage> TryReserveStockAsync(StockReservationRequest reservationRequest);
}