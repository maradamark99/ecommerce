using System.ComponentModel.DataAnnotations;

namespace Review.Contract;

public record ReviewRequestDto
(
    [Required] string ProductId,
    [Range(1, 5)] int Rating,
    [Required, MinLength(1)] string Comment
);