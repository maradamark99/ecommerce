namespace Ecommerce.Domain.OrderManagement.Contract;

public interface IOrderDataMapper
{
    public CreateOrderRequest MapCreateRequestDtoToModel(CreateRequestDto createRequestDto);
    public OrderSummaryResponseDto MapOrderSummaryToDto(OrderSummary orderSummary);
    
    public OrderResponseDto MapOrderToResponseDto(Order? order);
}