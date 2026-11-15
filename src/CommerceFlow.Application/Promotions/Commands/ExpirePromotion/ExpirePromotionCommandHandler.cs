using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Promotions.Interfaces;
using CommerceFlow.Domain.Entities.Promotions;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Promotions.Commands.ExpirePromotion;

public sealed class ExpirePromotionCommandHandler(
    IPromotionRepository promotions,
    IUnitOfWork unitOfWork) : IRequestHandler<ExpirePromotionCommand>
{
    public async Task Handle(ExpirePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await promotions.GetByIdAsync(request.PromotionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Promotion), request.PromotionId);

        // pre-check before the state moves (state conflicts -> 409): the
        // terminal state allows nothing out
        if (promotion.Status == PromotionStatus.Expired)
            throw new ConflictException(
                $"Only draft, active or paused promotions can be expired (current: {promotion.Status}).");

        promotion.Expire();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
