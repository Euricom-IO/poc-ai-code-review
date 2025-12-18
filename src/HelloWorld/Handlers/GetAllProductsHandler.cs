using HelloWorld.Data;
using HelloWorld.Models;
using HelloWorld.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelloWorld.Handlers;

/// <summary>
/// Handler for retrieving all products from the database.
/// </summary>
public class GetAllProductsHandler : IRequestHandler<GetAllProductsQuery, List<Product>>
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllProductsHandler"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public GetAllProductsHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Handles the GetAllProductsQuery request.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of all products.</returns>
    public async Task<List<Product>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Products
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }
}
