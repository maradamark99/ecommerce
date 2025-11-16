using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Domain.Review.Contract;

public record ReviewRequestDto
(
    [Required] string ProductId,
    [Range(1,5)] int Rating, 
    [MinLength(3)] [MaxLength(500)] string Comment
);