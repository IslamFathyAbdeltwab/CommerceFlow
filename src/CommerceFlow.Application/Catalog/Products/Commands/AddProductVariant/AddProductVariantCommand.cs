using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.AddProductVariant;

public sealed record AddProductVariantCommand(
    Guid ProductId,
    string Name,
    string Sku,
    decimal Price,
    decimal? CompareAtPrice = null) : IRequest<Guid>;
