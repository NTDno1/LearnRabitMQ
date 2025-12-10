using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Migrations;

/// <summary>
/// Script để tạo database và tables
/// Chạy lệnh này một lần để khởi tạo database
/// </summary>
public static class DatabaseMigration
{
    public static void MigrateDatabase(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            dbContext.Database.EnsureCreated();
            Console.WriteLine("Database created successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating database: {ex.Message}");
        }
    }
}

