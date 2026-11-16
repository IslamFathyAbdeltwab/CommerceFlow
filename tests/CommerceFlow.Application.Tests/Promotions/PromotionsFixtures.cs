using CommerceFlow.Application.Promotions.Commands.CreatePromotion;
using CommerceFlow.Application.Promotions.Commands.UpdatePromotion;
using CommerceFlow.Domain.Entities.Promotions;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Tests.Promotions;

// Shared factories for building campaigns (and their create/update commands).
// The window defaults to "open now": started yesterday, ending in 30 days -
// most tests only care about the status, not the calendar. Usages can only
// be recorded while Active, so seeding runs before a Paused target is
// reached (see the switch below).
internal static class PromotionsFixtures
{
    public static readonly Guid CustomerId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    public static readonly DateTimeOffset StartedAtUtc = DateTimeOffset.UtcNow.AddDays(-1);
    public static readonly DateTimeOffset EndsAtUtc = DateTimeOffset.UtcNow.AddDays(30);

    public static Promotion CreatePromotion(
        string code = "SAVE10",
        DiscountType discountType = DiscountType.Percentage,
        decimal discountValue = 10,
        PromotionStatus status = PromotionStatus.Draft,
        DateTimeOffset? startAtUtc = null,
        DateTimeOffset? endAtUtc = null,
        string? description = "Ten percent off.",
        decimal? minOrderAmount = null,
        int? maxUsesTotal = null,
        int? maxUsesPerCustomer = null,
        int seededUses = 0,
        Guid? seedCustomerId = null)
    {
        if (seededUses > 0 && status == PromotionStatus.Draft)
            throw new ArgumentException(
                "A draft was never published, so it cannot carry usages.",
                nameof(status));

        var promotion = Promotion.Create(
            code,
            "Autumn sale",
            discountType,
            discountValue,
            startAtUtc ?? StartedAtUtc,
            description,
            endAtUtc ?? EndsAtUtc,
            minOrderAmount,
            maxUsesTotal,
            maxUsesPerCustomer);

        if (status is not (PromotionStatus.Draft or PromotionStatus.Expired) || seededUses > 0)
        {
            // an already-ended window refuses to publish with the real clock -
            // backdate `now` so tests can still build the "active but ended"
            // state that redemption then refuses
            var publishAt = endAtUtc is { } end && end <= DateTimeOffset.UtcNow
                ? end.AddTicks(-1)
                : (DateTimeOffset?)null;
            promotion.Activate(publishAt);
        }

        for (var i = 0; i < seededUses; i++)
            promotion.RecordUsage(seedCustomerId ?? CustomerId, Guid.NewGuid(), 5m);

        switch (status)
        {
            case PromotionStatus.Draft:
            case PromotionStatus.Active:
                break;
            case PromotionStatus.Paused:
                promotion.Pause();
                break;
            case PromotionStatus.Expired:
                promotion.Expire();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        return promotion;
    }

    public static CreatePromotionCommand ValidCreateCommand(
        string code = "SAVE10",
        string name = "Autumn sale",
        DiscountType discountType = DiscountType.Percentage,
        decimal discountValue = 10,
        DateTimeOffset? startAtUtc = null,
        string? description = "Ten percent off.",
        DateTimeOffset? endAtUtc = null,
        decimal? minOrderAmount = null,
        int? maxUsesTotal = null,
        int? maxUsesPerCustomer = null) =>
        new(
            code,
            name,
            discountType,
            discountValue,
            startAtUtc ?? StartedAtUtc,
            description,
            endAtUtc ?? EndsAtUtc,
            minOrderAmount,
            maxUsesTotal,
            maxUsesPerCustomer);

    public static UpdatePromotionCommand ValidUpdateCommand(
        Guid promotionId,
        string name = "Autumn sale v2",
        string? description = "Ten percent off, revised.",
        DateTimeOffset? startAtUtc = null,
        DateTimeOffset? endAtUtc = null,
        decimal? minOrderAmount = null,
        int? maxUsesTotal = null,
        int? maxUsesPerCustomer = null) =>
        new(
            promotionId,
            name,
            description,
            startAtUtc ?? StartedAtUtc,
            endAtUtc ?? EndsAtUtc,
            minOrderAmount,
            maxUsesTotal,
            maxUsesPerCustomer);
}
