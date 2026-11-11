using FluentValidation;

namespace CommerceFlow.Application.Wishlists.Commands.ClearWishlist;

public sealed class ClearWishlistCommandValidator : AbstractValidator<ClearWishlistCommand>
{
    public ClearWishlistCommandValidator()
    {
        RuleFor(x => x.WishlistId).NotEmpty();
    }
}
