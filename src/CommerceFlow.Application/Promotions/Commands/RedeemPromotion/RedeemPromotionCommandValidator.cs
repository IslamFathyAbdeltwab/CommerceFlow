using FluentValidation;

namespace CommerceFlow.Application.Promotions.Commands.RedeemPromotion;

public sealed class RedeemPromotionCommandValidator : AbstractValidator<RedeemPromotionCommand>
{
    public RedeemPromotionCommandValidator()
    {
        RuleFor(x => x.PromotionId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();

        // shape only: eligibility (limits, window, status) is business
        // logic and lives in the handler against the domain
        RuleFor(x => x.OrderAmount).GreaterThanOrEqualTo(0);
    }
}
