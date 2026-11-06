using CommerceFlow.Domain.Entities.Catalog;
using FluentValidation;

namespace CommerceFlow.Application.Catalog.Products.Commands.AddProductVariant;

public sealed class AddProductVariantCommandValidator : AbstractValidator<AddProductVariantCommand>
{
    public AddProductVariantCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(ProductVariant.NameMaxLength);

        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(ProductVariant.SkuMaxLength);

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);

        RuleFor(x => x.CompareAtPrice)
            .GreaterThan(x => x.Price)
            .WithMessage("Compare-at price must be greater than the price.")
            .When(x => x.CompareAtPrice.HasValue);
    }
}
