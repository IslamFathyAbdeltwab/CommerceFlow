using FluentValidation;

namespace CommerceFlow.Application.Catalog.Products.Queries.GetFeaturedProducts;

public sealed class GetFeaturedProductsQueryValidator : AbstractValidator<GetFeaturedProductsQuery>
{
    public GetFeaturedProductsQueryValidator()
    {
        RuleFor(x => x.Take).InclusiveBetween(1, 50);
    }
}