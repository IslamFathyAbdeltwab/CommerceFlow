using FluentValidation;

namespace CommerceFlow.Application.Catalog.Products.Queries.GetProducts;

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .When(x => x.SearchTerm is not null);

        RuleFor(x => x.BrandId).NotEqual(Guid.Empty).When(x => x.BrandId.HasValue);
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty).When(x => x.CategoryId.HasValue);

        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MaxPrice >= x.MinPrice)
            .WithMessage("MaxPrice must be greater than or equal to MinPrice.");

        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}