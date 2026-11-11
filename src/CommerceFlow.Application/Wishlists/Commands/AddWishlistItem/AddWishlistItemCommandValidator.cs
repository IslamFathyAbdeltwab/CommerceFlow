using FluentValidation;

namespace CommerceFlow.Application.Wishlists.Commands.AddWishlistItem;

public sealed class AddWishlistItemCommandValidator : AbstractValidator<AddWishlistItemCommand>
{
    public AddWishlistItemCommandValidator()
    {
        RuleFor(x => x.WishlistId).NotEmpty();
        RuleFor(x => x.ProductVariantId).NotEmpty();
    }
}
