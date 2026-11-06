using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.ChangeVariantPrice;

public sealed record ChangeVariantPriceCommand(
    Guid ProductId,
    Guid VariantId,
    decimal Price,
    decimal? CompareAtPrice = null) : IRequest;
