using FluentValidation;

namespace CommerceFlow.Application.Catalog.Categories.Queries.GetCategories;

public sealed class GetCategoriesQueryValidator : AbstractValidator<GetCategoriesQuery>
{
    public GetCategoriesQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .When(x => x.SearchTerm is not null);

        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
