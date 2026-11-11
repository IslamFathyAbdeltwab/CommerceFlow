using FluentValidation;

namespace CommerceFlow.Application.Wishlists.Commands.CreateWishlist;

public sealed class CreateWishlistCommandValidator : AbstractValidator<CreateWishlistCommand>
{
    public CreateWishlistCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}
