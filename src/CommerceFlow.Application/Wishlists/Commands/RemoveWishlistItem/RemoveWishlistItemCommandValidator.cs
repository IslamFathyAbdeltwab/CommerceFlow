using FluentValidation;

namespace CommerceFlow.Application.Wishlists.Commands.RemoveWishlistItem;

public sealed class RemoveWishlistItemCommandValidator : AbstractValidator<RemoveWishlistItemCommand>
{
    public RemoveWishlistItemCommandValidator()
    {
        RuleFor(x => x.WishlistId).NotEmpty();
        RuleFor(x => x.WishlistItemId).NotEmpty();
    }
}
