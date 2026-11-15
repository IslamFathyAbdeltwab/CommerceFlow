using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Promotions.Interfaces;
using CommerceFlow.Domain.Entities.Promotions;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Promotions.Commands.ActivatePromotion;

public sealed class ActivatePromotionCommandHandler(
    IPromotionRepository promotions,
    IUnitOfWork unitOfWork) : IRequestHandler<ActivatePromotionCommand>
{
    public async Task Handle(ActivatePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await promotions.GetByIdAsync(request.PromotionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Promotion), request.PromotionId);

        // pre-check before the state moves (state conflicts -> 409)
        if (promotion.Status is not (PromotionStatus.Draft or PromotionStatus.Paused))
            throw new ConflictException(
                $"Only draft or paused promotions can be activated (current: {promotion.Status}).");

        // the window itself: well-formed call, wrong timing -> 409 (the
        // domain repeats this guard as its safety net with the same clock)
        if (promotion.EndAtUtc is { } end && DateTimeOffset.UtcNow >= end)
            throw new ConflictException("Promotion window has already ended.");

        promotion.Activate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
