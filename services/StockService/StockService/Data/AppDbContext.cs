using Microsoft.EntityFrameworkCore;
using StockService.Models;

namespace StockService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
}