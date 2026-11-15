using FluentValidation;

namespace CommerceFlow.Application.Promotions.Commands.ActivatePromotion;

public sealed class ActivatePromotionCommandValidator : AbstractValidator<ActivatePromotionCommand>
{
    public ActivatePromotionCommandValidator()
    {
        RuleFor(x => x.PromotionId).NotEmpty();
    }
}
