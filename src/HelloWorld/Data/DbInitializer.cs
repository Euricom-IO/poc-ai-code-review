using HelloWorld.Models;
using Microsoft.EntityFrameworkCore;

namespace HelloWorld.Data;

/// <summary>
/// Initializes the database with seed data.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Initializes the database asynchronously and seeds initial data if the database is empty.
    /// </summary>
    /// <param name="context">The database context.</param>
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        // Ensure the database is created
        await context.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await context.Products.AnyAsync())
        {
            return; // Database has been seeded
        }

        // Seed products
        var products = new[]
        {
            new Product
            {
                Name = "Laptop",
                Description = "High-performance laptop with 16GB RAM",
                Price = 1299.99m,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Name = "Wireless Mouse",
                Description = null,
                Price = 29.99m,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Name = "USB-C Cable",
                Description = "3-meter charging cable",
                Price = 15.99m,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Name = "External SSD",
                Description = "1TB portable storage",
                Price = 129.99m,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Name = "Webcam",
                Description = "1080p HD webcam with microphone",
                Price = 79.99m,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
