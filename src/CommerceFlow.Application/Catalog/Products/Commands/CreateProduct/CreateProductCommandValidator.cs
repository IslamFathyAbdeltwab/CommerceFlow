using CommerceFlow.Domain.Entities.Catalog;
using FluentValidation;

namespace CommerceFlow.Application.Catalog.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Product.NameMaxLength);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(Product.SlugMaxLength)
            .Matches("^[a-zA-Z0-9]+(?:-[a-zA-Z0-9]+)*$")
            .WithMessage("Slug can contain only letters, numbers and single hyphens.");

        RuleFor(x => x.Description)
            .NotEmpty();

        RuleFor(x => x.BrandId)
            .NotEqual(Guid.Empty)
            .When(x => x.BrandId.HasValue);

        RuleForEach(x => x.CategoryIds)
            .NotEmpty()
            .When(x => x.CategoryIds is not null);
    }
}