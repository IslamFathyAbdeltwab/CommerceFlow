using CommerceFlow.Domain.Entities.Catalog;
using FluentValidation;

namespace CommerceFlow.Application.Catalog.Products.Commands.SetVariantAttributeValue;

public sealed class SetVariantAttributeValueCommandValidator : AbstractValidator<SetVariantAttributeValueCommand>
{
    public SetVariantAttributeValueCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.AttributeId).NotEmpty();

        RuleFor(x => x.Value)
            .NotEmpty()
            .MaximumLength(ProductVariantAttributeValue.ValueMaxLength);
    }
}
