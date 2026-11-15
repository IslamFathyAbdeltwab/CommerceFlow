using FluentValidation;

namespace CommerceFlow.Application.Promotions.Commands.ExpirePromotion;

public sealed class ExpirePromotionCommandValidator : AbstractValidator<ExpirePromotionCommand>
{
    public ExpirePromotionCommandValidator()
    {
        RuleFor(x => x.PromotionId).NotEmpty();
    }
}
