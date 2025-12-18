using HelloWorld.Data;
using HelloWorld.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

try
{
    // Create host with dependency injection
    var builder = Host.CreateApplicationBuilder(args);

    // Configure services
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite("Data Source=products.db"));

    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

    builder.Logging.AddConsole();

    // Build host
    var host = builder.Build();

    // Initialize database
    Console.WriteLine("Initializing database...");
    using (var scope = host.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await DbInitializer.InitializeAsync(context);
    }
    Console.WriteLine("Database initialized successfully.\n");

    // Execute queries
    using (var scope = host.Services.CreateScope())
    {
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Query 1: Get all products
        Console.WriteLine("Fetching all products...");
        var allProducts = await mediator.Send(new GetAllProductsQuery());
        Console.WriteLine($"Found {allProducts.Count} products:");
        foreach (var product in allProducts)
        {
            Console.WriteLine($"  [{product.Id}] {product.Name} - ${product.Price:N2}");
            if (!string.IsNullOrEmpty(product.Description))
            {
                Console.WriteLine($"      Description: {product.Description}");
            }
        }

        Console.WriteLine();

        // Query 2: Get product by ID
        Console.WriteLine("Fetching product with ID 1...");
        var singleProduct = await mediator.Send(new GetProductByIdQuery(1));
        if (singleProduct != null)
        {
            Console.WriteLine($"Product: {singleProduct.Name}");
            Console.WriteLine($"  Price: ${singleProduct.Price:N2}");
            Console.WriteLine($"  Description: {singleProduct.Description ?? "N/A"}");
            Console.WriteLine($"  Created: {singleProduct.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        }
        else
        {
            Console.WriteLine("Product not found.");
        }
    }

    Console.WriteLine("\nApplication completed successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    return 1;
}

return 0;
