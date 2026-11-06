using CommerceFlow.Domain.Entities.Catalog;
using FluentValidation;

namespace CommerceFlow.Application.Catalog.Products.Commands.AddProductImage;

public sealed class AddProductImageCommandValidator : AbstractValidator<AddProductImageCommand>
{
    public AddProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.StorageKey)
            .NotEmpty()
            .MaximumLength(ProductImage.StorageKeyMaxLength);

        RuleFor(x => x.AltText)
            .MaximumLength(ProductImage.AltTextMaxLength)
            .When(x => x.AltText is not null);

        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);

        RuleFor(x => x.VariantId)
            .NotEqual(Guid.Empty)
            .When(x => x.VariantId.HasValue);

        RuleFor(x => x)
            .Must(x => !x.IsPrimary || x.VariantId is null)
            .WithMessage("The primary image must be a product-level image, not a variant image.");
    }
}
