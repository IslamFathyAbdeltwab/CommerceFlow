using CommerceFlow.Domain.Entities.Catalog;
using FluentValidation;

namespace CommerceFlow.Application.Catalog.Products.Commands.UpdateProductVariant;

public sealed class UpdateProductVariantCommandValidator : AbstractValidator<UpdateProductVariantCommand>
{
    public UpdateProductVariantCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.VariantId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(ProductVariant.NameMaxLength);

        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(ProductVariant.SkuMaxLength);

        RuleFor(x => x.Barcode)
            .MaximumLength(ProductVariant.BarcodeMaxLength)
            .When(x => x.Barcode is not null);

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);

        RuleFor(x => x.CompareAtPrice)
            .GreaterThan(x => x.Price)
            .WithMessage("Compare-at price must be greater than the price.")
            .When(x => x.CompareAtPrice.HasValue);

        RuleFor(x => x.WeightGrams)
            .GreaterThanOrEqualTo(0)
            .When(x => x.WeightGrams.HasValue);
    }
}
