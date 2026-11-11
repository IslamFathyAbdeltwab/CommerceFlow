using MediatR;

namespace CommerceFlow.Application.Wishlists.Commands.CreateWishlist;

public sealed record CreateWishlistCommand(Guid CustomerId) : IRequest<Guid>;
