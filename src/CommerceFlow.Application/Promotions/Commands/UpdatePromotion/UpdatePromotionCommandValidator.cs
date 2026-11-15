using CommerceFlow.Domain.Entities.Promotions;
using FluentValidation;

namespace CommerceFlow.Application.Promotions.Commands.UpdatePromotion;

public sealed class UpdatePromotionCommandValidator : AbstractValidator<UpdatePromotionCommand>
{
    public UpdatePromotionCommandValidator()
    {
        RuleFor(x => x.PromotionId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Promotion.NameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(Promotion.DescriptionMaxLength);

        RuleFor(x => x.StartAtUtc).NotEmpty();

        // shape only - relational rules live in the domain (same split as
        // CreatePromotionCommandValidator)
        RuleFor(x => x.MinOrderAmount).GreaterThan(0).When(x => x.MinOrderAmount.HasValue);
        RuleFor(x => x.MaxUsesTotal).GreaterThan(0).When(x => x.MaxUsesTotal.HasValue);
        RuleFor(x => x.MaxUsesPerCustomer).GreaterThan(0).When(x => x.MaxUsesPerCustomer.HasValue);
    }
}
