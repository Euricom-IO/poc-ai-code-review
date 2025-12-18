using HelloWorld.Data;
using HelloWorld.Models;
using HelloWorld.Queries;
using MediatR;

namespace HelloWorld.Handlers;

/// <summary>
/// Handler for retrieving a product by its identifier.
/// </summary>
public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, Product?>
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetProductByIdHandler"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public GetProductByIdHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Handles the GetProductByIdQuery request.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The product if found; otherwise, null.</returns>
    public async Task<Product?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Products.FindAsync(new object[] { request.Id }, cancellationToken);
    }
}
