using CommerceFlow.Domain.Entities.Promotions;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Promotions.Queries;

// One DTO for detail and list reads (same call as ReturnRequestDto).
// Redemption rows stay out: they are audit data, not campaign shape - the
// campaign's CurrentUses already summarises them.
public sealed record PromotionDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    PromotionStatus Status,
    DateTimeOffset StartAtUtc,
    DateTimeOffset? EndAtUtc,
    decimal? MinOrderAmount,
    int? MaxUsesTotal,
    int? MaxUsesPerCustomer,
    int CurrentUses,
    DateTimeOffset? ActivatedAtUtc,
    DateTimeOffset? PausedAtUtc,
    DateTimeOffset? ExpiredAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

// Centralizes entity -> DTO mapping so the handlers stay thin.
internal static class PromotionMappings
{
    public static PromotionDto ToDto(Promotion promotion) => new(
        promotion.Id,
        promotion.Code,
        promotion.Name,
        promotion.Description,
        promotion.DiscountType,
        promotion.DiscountValue,
        promotion.Status,
        promotion.StartAtUtc,
        promotion.EndAtUtc,
        promotion.MinOrderAmount,
        promotion.MaxUsesTotal,
        promotion.MaxUsesPerCustomer,
        promotion.CurrentUses,
        promotion.ActivatedAtUtc,
        promotion.PausedAtUtc,
        promotion.ExpiredAtUtc,
        promotion.CreatedAtUtc,
        promotion.UpdatedAtUtc);
}
