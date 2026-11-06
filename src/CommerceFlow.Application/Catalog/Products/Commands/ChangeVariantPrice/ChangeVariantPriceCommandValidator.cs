using FluentValidation;

namespace CommerceFlow.Application.Catalog.Products.Commands.ChangeVariantPrice;

public sealed class ChangeVariantPriceCommandValidator : AbstractValidator<ChangeVariantPriceCommand>
{
    public ChangeVariantPriceCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.VariantId).NotEmpty();

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);

        RuleFor(x => x.CompareAtPrice)
            .GreaterThan(x => x.Price)
            .WithMessage("Compare-at price must be greater than the price.")
            .When(x => x.CompareAtPrice.HasValue);
    }
}
