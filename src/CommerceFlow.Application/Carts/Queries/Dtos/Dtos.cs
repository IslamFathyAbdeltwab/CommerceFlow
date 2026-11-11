using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Carts.Queries;

public sealed record CartDto(
    Guid Id,
    Guid CustomerId,
    CartStatus Status,
    int LineCount,
    int TotalQuantity,
    IReadOnlyList<CartLineDto> Lines,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CartLineDto(
    Guid Id,
    Guid ProductVariantId,
    int Quantity,
    DateTimeOffset AddedAtUtc);

public sealed record CartListItemDto(
    Guid Id,
    Guid CustomerId,
    CartStatus Status,
    int LineCount,
    int TotalQuantity,
    DateTimeOffset UpdatedAtUtc);

// Centralizes entity -> DTO mapping so the query handlers stay thin.
internal static class CartMappings
{
    public static CartDto ToDto(Cart cart) => new(
        cart.Id,
        cart.CustomerId,
        cart.Status,
        cart.LineCount,
        cart.TotalQuantity,
        cart.Lines
            .Select(l => new CartLineDto(l.Id, l.ProductVariantId, l.Quantity, l.AddedAtUtc))
            .ToList(),
        cart.CreatedAtUtc,
        cart.UpdatedAtUtc);

    public static CartListItemDto ToListItem(Cart cart) => new(
        cart.Id,
        cart.CustomerId,
        cart.Status,
        cart.LineCount,
        cart.TotalQuantity,
        cart.UpdatedAtUtc);
}
