using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Promotions.Interfaces;
using CommerceFlow.Domain.Entities.Promotions;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Promotions.Commands.PausePromotion;

public sealed class PausePromotionCommandHandler(
    IPromotionRepository promotions,
    IUnitOfWork unitOfWork) : IRequestHandler<PausePromotionCommand>
{
    public async Task Handle(PausePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await promotions.GetByIdAsync(request.PromotionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Promotion), request.PromotionId);

        // pre-check before the state moves (state conflicts -> 409): a
        // campaign can only be taken offline from Active
        if (promotion.Status != PromotionStatus.Active)
            throw new ConflictException(
                $"Only active promotions can be paused (current: {promotion.Status}).");

        promotion.Pause();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
