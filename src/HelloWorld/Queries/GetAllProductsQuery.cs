using HelloWorld.Models;
using MediatR;

namespace HelloWorld.Queries;

/// <summary>
/// Query to retrieve all products from the database.
/// </summary>
public record GetAllProductsQuery : IRequest<List<Product>>;
