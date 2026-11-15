using FluentValidation;

namespace CommerceFlow.Application.Promotions.Commands.PausePromotion;

public sealed class PausePromotionCommandValidator : AbstractValidator<PausePromotionCommand>
{
    public PausePromotionCommandValidator()
    {
        RuleFor(x => x.PromotionId).NotEmpty();
    }
}
