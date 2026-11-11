using FluentValidation;

namespace CommerceFlow.Application.Wishlists.Queries.GetWishlists;

public sealed class GetWishlistsQueryValidator : AbstractValidator<GetWishlistsQuery>
{
    public GetWishlistsQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
