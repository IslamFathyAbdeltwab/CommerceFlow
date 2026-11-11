using FluentValidation;

namespace CommerceFlow.Application.Carts.Queries.GetCarts;

public sealed class GetCartsQueryValidator : AbstractValidator<GetCartsQuery>
{
    public GetCartsQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
