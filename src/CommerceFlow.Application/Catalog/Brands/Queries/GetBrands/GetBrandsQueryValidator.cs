using FluentValidation;

namespace CommerceFlow.Application.Catalog.Brands.Queries.GetBrands;

public sealed class GetBrandsQueryValidator : AbstractValidator<GetBrandsQuery>
{
    public GetBrandsQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .When(x => x.SearchTerm is not null);

        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
