using HelloWorld.Models;
using MediatR;

namespace HelloWorld.Queries;

/// <summary>
/// Query to retrieve a product by its unique identifier.
/// </summary>
/// <param name="Id">The unique identifier of the product to retrieve.</param>
public record GetProductByIdQuery(int Id) : IRequest<Product?>;
