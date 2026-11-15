using MediatR;

namespace CommerceFlow.Application.Promotions.Commands.ExpirePromotion;

// Force-ends a campaign from any non-terminal state (the merchant's kill
// switch). Time-expired campaigns also reach Expired through ExpireIfDue;
// Expired is terminal - a new campaign afterwards is a new promotion.
public sealed record ExpirePromotionCommand(Guid PromotionId) : IRequest;
