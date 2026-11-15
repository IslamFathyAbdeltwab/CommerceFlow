using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Promotions.Interfaces;
using CommerceFlow.Domain.Entities.Promotions;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Promotions.Commands.UpdatePromotion;

public sealed class UpdatePromotionCommandHandler(
    IPromotionRepository promotions,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdatePromotionCommand>
{
    public async Task Handle(UpdatePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await promotions.GetByIdAsync(request.PromotionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Promotion), request.PromotionId);

        // pre-check before anything changes (state conflicts -> 409): live
        // and ended campaigns are read-only
        if (promotion.Status is not (PromotionStatus.Draft or PromotionStatus.Paused))
            throw new ConflictException(
                $"Only draft or paused promotions can be updated (current: {promotion.Status}).");

        promotion.UpdateDetails(
            request.Name,
            request.Description,
            request.StartAtUtc,
            request.EndAtUtc,
            request.MinOrderAmount,
            request.MaxUsesTotal,
            request.MaxUsesPerCustomer);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
