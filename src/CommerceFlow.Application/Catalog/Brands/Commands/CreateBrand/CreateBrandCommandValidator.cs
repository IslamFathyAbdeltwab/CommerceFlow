using CommerceFlow.Domain.Entities.Catalog;
using FluentValidation;

namespace CommerceFlow.Application.Catalog.Brands.Commands.CreateBrand;

public sealed class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Brand.NameMaxLength);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(Brand.SlugMaxLength)
            .Matches("^[a-zA-Z0-9]+(?:-[a-zA-Z0-9]+)*$")
            .WithMessage("Slug can contain only letters, numbers and single hyphens.");

        RuleFor(x => x.Description)
            .MaximumLength(Brand.DescriptionMaxLength)
            .When(x => x.Description is not null);

        RuleFor(x => x.LogoUrl)
            .MaximumLength(Brand.LogoUrlMaxLength)
            .When(x => x.LogoUrl is not null);
    }
}
