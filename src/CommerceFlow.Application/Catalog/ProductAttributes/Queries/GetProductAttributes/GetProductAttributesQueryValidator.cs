using FluentValidation;

namespace CommerceFlow.Application.Catalog.ProductAttributes.Queries.GetProductAttributes;

public sealed class GetProductAttributesQueryValidator : AbstractValidator<GetProductAttributesQuery>
{
    public GetProductAttributesQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .When(x => x.SearchTerm is not null);

        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
