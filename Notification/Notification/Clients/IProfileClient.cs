namespace Notification.Clients;

public interface IProfileClient
{
    Task<CustomerDetailsDto?> GetCustomerDetailsAsync(string customerId);
}