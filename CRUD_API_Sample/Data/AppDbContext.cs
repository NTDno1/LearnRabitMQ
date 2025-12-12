using CRUD_API_Sample.Models;
using Microsoft.EntityFrameworkCore;

namespace CRUD_API_Sample.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Item> Items => Set<Item>();
}

