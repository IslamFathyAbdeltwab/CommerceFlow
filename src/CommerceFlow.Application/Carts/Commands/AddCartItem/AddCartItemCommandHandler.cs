using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Carts.Interfaces;
using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Carts.Commands.AddCartItem;

public sealed class AddCartItemCommandHandler(
    ICartRepository carts,
    IUnitOfWork unitOfWork) : IRequestHandler<AddCartItemCommand, Guid>
{
    public async Task<Guid> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await carts.GetByIdAsync(request.CartId, cancellationToken)
            ?? throw new NotFoundException(nameof(Cart), request.CartId);

        if (cart.Status != CartStatus.Active)
            throw new ConflictException("Cart items can only be changed while the cart is active.");

        // NOTE: variant existence is enforced by the Infrastructure FK (same
        // decision as Inventory items) - Application must not cross into Catalog.
        if (cart.LineCount >= Cart.MaxLines
            && cart.Lines.All(l => l.ProductVariantId != request.ProductVariantId))
            throw new ConflictException($"A cart cannot have more than {Cart.MaxLines} lines.");

        var line = cart.AddItem(request.ProductVariantId, request.Quantity);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return line.Id;
    }
}
