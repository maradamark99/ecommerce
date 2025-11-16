namespace Inventory.Contract;

public record StockReservationRequest(string OrderId, List<StockReservationItem> Items);