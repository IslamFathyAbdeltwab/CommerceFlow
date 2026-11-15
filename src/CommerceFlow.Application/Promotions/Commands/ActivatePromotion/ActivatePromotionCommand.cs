using MediatR;

namespace CommerceFlow.Application.Promotions.Commands.ActivatePromotion;

// Publishes a Draft campaign or resumes a Paused one - the merchant's half
// of going live. A future start date is fine (the campaign redeems only
// once the window opens); an ended window is refused with 409.
public sealed record ActivatePromotionCommand(Guid PromotionId) : IRequest;
