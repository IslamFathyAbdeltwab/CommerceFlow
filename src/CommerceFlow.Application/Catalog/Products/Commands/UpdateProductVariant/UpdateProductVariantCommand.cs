using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.UpdateProductVariant;

public sealed record UpdateProductVariantCommand(
    Guid ProductId,
    Guid VariantId,
    string Name,
    string Sku,
    decimal Price,
    string? Barcode = null,
    decimal? CompareAtPrice = null,
    int? WeightGrams = null) : IRequest;
