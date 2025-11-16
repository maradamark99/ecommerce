using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Domain.ProductManagement.ProductManagement.Contract;

public record ProductListingRequest(
    [Required(ErrorMessage = "ProductId is required.")]
    string ProductId,

    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    decimal PriceInEur
);