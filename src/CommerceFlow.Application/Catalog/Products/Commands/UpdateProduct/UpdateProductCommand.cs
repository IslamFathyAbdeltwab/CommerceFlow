using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Slug,
    string Description,
    Guid? BrandId) : IRequest;
