using CommerceFlow.Domain.Entities.Promotions;
using CommerceFlow.Domain.Enums;
using FluentValidation;

namespace CommerceFlow.Application.Promotions.Commands.CreatePromotion;

public sealed class CreatePromotionCommandValidator : AbstractValidator<CreatePromotionCommand>
{
    public CreatePromotionCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(Promotion.CodeMaxLength);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Promotion.NameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(Promotion.DescriptionMaxLength);

        RuleFor(x => x.DiscountType).IsInEnum();
        RuleFor(x => x.DiscountValue).GreaterThan(0);

        // a percentage above 100 would cut more than the order costs - plain
        // range of the field; the order-amount cap itself stays in
        // CalculateDiscount
        RuleFor(x => x.DiscountValue)
            .LessThanOrEqualTo(100)
            .When(x => x.DiscountType == DiscountType.Percentage);

        RuleFor(x => x.StartAtUtc).NotEmpty();

        // shape only: relational rules (end after start, per-customer <=
        // total, limits vs uses already recorded) are business logic and
        // live in the domain
        RuleFor(x => x.MinOrderAmount).GreaterThan(0).When(x => x.MinOrderAmount.HasValue);
        RuleFor(x => x.MaxUsesTotal).GreaterThan(0).When(x => x.MaxUsesTotal.HasValue);
        RuleFor(x => x.MaxUsesPerCustomer).GreaterThan(0).When(x => x.MaxUsesPerCustomer.HasValue);
    }
}
