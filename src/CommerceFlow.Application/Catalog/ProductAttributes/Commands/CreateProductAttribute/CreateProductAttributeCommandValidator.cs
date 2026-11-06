using CommerceFlow.Domain.Entities.Catalog;
using FluentValidation;

namespace CommerceFlow.Application.Catalog.ProductAttributes.Commands.CreateProductAttribute;

public sealed class CreateProductAttributeCommandValidator : AbstractValidator<CreateProductAttributeCommand>
{
    public CreateProductAttributeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(ProductAttribute.NameMaxLength);

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(ProductAttribute.CodeMaxLength)
            .Matches("^[a-zA-Z0-9]+(?:-[a-zA-Z0-9]+)*$")
            .WithMessage("Code can contain only letters, numbers and single hyphens.");

        RuleFor(x => x.DataType).IsInEnum();
    }
}
