using MediatR;

namespace CommerceFlow.Application.Promotions.Commands.PausePromotion;

// Merchant takes a live campaign offline temporarily; it can be resumed
// later with ActivatePromotion (Paused -> Active is a legal move).
public sealed record PausePromotionCommand(Guid PromotionId) : IRequest;
