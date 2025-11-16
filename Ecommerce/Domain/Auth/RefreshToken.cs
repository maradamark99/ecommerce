using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Domain.Auth;

public class RefreshToken
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(128)]
    public string Token { get; set; }
    
    public bool IsRevoked { get; set; }

    public string? CreatedByIp { get; set; }
    
    public DateTime ExpiresAtUtc { get; set; }
    
    [Required]
    public AppUser User { get; set; }
}