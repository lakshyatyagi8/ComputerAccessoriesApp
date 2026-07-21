using Accessories.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Accessories.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Product> Products { get; set; }
}