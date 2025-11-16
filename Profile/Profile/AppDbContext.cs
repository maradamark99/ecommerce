using Microsoft.EntityFrameworkCore;
using Profile.Data;

namespace Profile;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<CustomerDetails> CustomerDetails { get; set; }
    
    
    public DbSet<Address> Addresses { get; set; }
}