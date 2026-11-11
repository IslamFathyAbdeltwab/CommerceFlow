using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Carts.Interfaces;
using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Carts.Commands.ChangeCartItemQuantity;

public sealed class ChangeCartItemQuantityCommandHandler(
    ICartRepository carts,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeCartItemQuantityCommand>
{
    public async Task Handle(ChangeCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        var cart = await carts.GetByIdAsync(request.CartId, cancellationToken)
            ?? throw new NotFoundException(nameof(Cart), request.CartId);

        if (cart.Status != CartStatus.Active)
            throw new ConflictException("Cart items can only be changed while the cart is active.");

        if (cart.Lines.All(l => l.Id != request.CartLineId))
            throw new NotFoundException(nameof(CartLine), request.CartLineId);

        cart.ChangeQuantity(request.CartLineId, request.Quantity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
