using MediatR;

namespace CommerceFlow.Application.Promotions.Commands.RedeemPromotion;

// Applies the campaign to one order: checks eligibility (status, window,
// both usage limits, minimum order amount), computes the discount and
// records the redemption. The order's own discount column is set by the
// Cart/Order integration later - this command owns the campaign side.
// Returns what the campaign took off the order and where its total stands.
public sealed record RedeemPromotionCommand(
    Guid PromotionId,
    Guid CustomerId,
    Guid OrderId,
    decimal OrderAmount) : IRequest<RedeemPromotionResultDto>;

public sealed record RedeemPromotionResultDto(
    decimal DiscountTaken,
    int CurrentUses);
