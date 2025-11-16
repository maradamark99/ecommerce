namespace OrderManagement.Contract;

public record CustomerDetailsDto(
    string FullName,
    string Email,
    string PhoneNumber
);