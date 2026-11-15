using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Promotions.Interfaces;
using CommerceFlow.Domain.Entities.Promotions;
using CommerceFlow.Domain.Exceptions;
using MediatR;

namespace CommerceFlow.Application.Promotions.Commands.RedeemPromotion;

public sealed class RedeemPromotionCommandHandler(
    IPromotionRepository promotions,
    IUnitOfWork unitOfWork) : IRequestHandler<RedeemPromotionCommand, RedeemPromotionResultDto>
{
    public async Task<RedeemPromotionResultDto> Handle(RedeemPromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await promotions.GetByIdAsync(request.PromotionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Promotion), request.PromotionId);

        decimal discountTaken;
        try
        {
            // eligibility (status, window, both limits, minimum order) and
            // the amount are the domain's call. Every failure left here is a
            // business conflict - the campaign exists but refuses this
            // redemption - so it surfaces as 409, never 400 (the validator
            // already turned shape problems into 400s before we got here).
            promotion.EnsureCanRedeem(request.CustomerId, request.OrderAmount);

            discountTaken = promotion.CalculateDiscount(request.OrderAmount);

            // safety net: re-checks the shared core and appends the audit row
            promotion.RecordUsage(request.CustomerId, request.OrderId, discountTaken);
        }
        catch (DomainException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RedeemPromotionResultDto(discountTaken, promotion.CurrentUses);
    }
}
