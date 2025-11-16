namespace Wishlist.Contract;

public record ProductDto(
    string Id,
    string Name,
    decimal Price,
    string PrimaryImageUrl,
    bool IsAvailable);