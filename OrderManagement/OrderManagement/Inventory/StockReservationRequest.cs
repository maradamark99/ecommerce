namespace OrderManagement.Inventory;

public record StockReservationRequest(string OrderId, List<StockReservationItem> Items);