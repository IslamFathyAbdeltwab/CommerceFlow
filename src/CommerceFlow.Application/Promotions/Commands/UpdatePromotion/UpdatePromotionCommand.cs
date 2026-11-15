using MediatR;

namespace CommerceFlow.Application.Promotions.Commands.UpdatePromotion;

// Edits the tunable half of a campaign: text, window and limits. Allowed
// only while Draft or Paused (the handler pre-checks); code and discount
// stay frozen after creation (see Promotion.UpdateDetails).
public sealed record UpdatePromotionCommand(
    Guid PromotionId,
    string Name,
    string? Description,
    DateTimeOffset StartAtUtc,
    DateTimeOffset? EndAtUtc,
    decimal? MinOrderAmount,
    int? MaxUsesTotal,
    int? MaxUsesPerCustomer) : IRequest;
