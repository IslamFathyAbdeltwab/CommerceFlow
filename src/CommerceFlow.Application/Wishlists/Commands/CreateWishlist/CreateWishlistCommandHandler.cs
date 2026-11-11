using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Wishlists.Interfaces;
using CommerceFlow.Domain.Entities.Wishlists;
using MediatR;

namespace CommerceFlow.Application.Wishlists.Commands.CreateWishlist;

public sealed class CreateWishlistCommandHandler(
    IWishlistRepository wishlists,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateWishlistCommand, Guid>
{
    public async Task<Guid> Handle(CreateWishlistCommand request, CancellationToken cancellationToken)
    {
        // One wishlist per customer (also backed by a unique index in Infrastructure).
        if (await wishlists.GetByCustomerIdAsync(request.CustomerId, cancellationToken) is not null)
            throw new ConflictException($"Customer '{request.CustomerId}' already has a wishlist.");

        var wishlist = Wishlist.Create(request.CustomerId);

        await wishlists.AddAsync(wishlist, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return wishlist.Id;
    }
}
