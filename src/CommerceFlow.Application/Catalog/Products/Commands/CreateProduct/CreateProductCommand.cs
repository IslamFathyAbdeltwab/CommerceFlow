using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Slug,
    string Description,
    Guid? BrandId = null,
    IReadOnlyCollection<Guid>? CategoryIds = null) : IRequest<Guid>;