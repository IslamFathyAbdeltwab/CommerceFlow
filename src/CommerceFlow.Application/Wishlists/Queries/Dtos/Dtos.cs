using CommerceFlow.Domain.Entities.Wishlists;

namespace CommerceFlow.Application.Wishlists.Queries;

public sealed record WishlistDto(
    Guid Id,
    Guid CustomerId,
    int ItemCount,
    IReadOnlyList<WishlistItemDto> Items,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record WishlistItemDto(
    Guid Id,
    Guid ProductVariantId,
    DateTimeOffset AddedAtUtc);

public sealed record WishlistListItemDto(
    Guid Id,
    Guid CustomerId,
    int ItemCount,
    DateTimeOffset UpdatedAtUtc);

// Centralizes entity -> DTO mapping so the query handlers stay thin.
internal static class WishlistMappings
{
    public static WishlistDto ToDto(Wishlist wishlist) => new(
        wishlist.Id,
        wishlist.CustomerId,
        wishlist.ItemCount,
        wishlist.Items
            .Select(i => new WishlistItemDto(i.Id, i.ProductVariantId, i.AddedAtUtc))
            .ToList(),
        wishlist.CreatedAtUtc,
        wishlist.UpdatedAtUtc);

    public static WishlistListItemDto ToListItem(Wishlist wishlist) => new(
        wishlist.Id,
        wishlist.CustomerId,
        wishlist.ItemCount,
        wishlist.UpdatedAtUtc);
}
