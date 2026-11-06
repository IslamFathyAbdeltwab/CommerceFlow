using CommerceFlow.Domain.Entities.Catalog;
using FluentValidation;

namespace CommerceFlow.Application.Catalog.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Category.NameMaxLength);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(Category.SlugMaxLength)
            .Matches("^[a-zA-Z0-9]+(?:-[a-zA-Z0-9]+)*$")
            .WithMessage("Slug can contain only letters, numbers and single hyphens.");

        RuleFor(x => x.Description)
            .MaximumLength(Category.DescriptionMaxLength)
            .When(x => x.Description is not null);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.ParentCategoryId)
            .NotEqual(Guid.Empty)
            .When(x => x.ParentCategoryId.HasValue);

        RuleFor(x => x.ParentCategoryId)
            .NotEqual(x => x.CategoryId)
            .When(x => x.ParentCategoryId.HasValue)
            .WithMessage("A category cannot be its own parent.");
    }
}
