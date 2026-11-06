using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.AddProductImage;

public sealed record AddProductImageCommand(
    Guid ProductId,
    string StorageKey,
    string? AltText,
    int DisplayOrder,
    bool IsPrimary = false,
    Guid? VariantId = null) : IRequest<Guid>;
