using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Carts.Interfaces;
using CommerceFlow.Domain.Entities.Carts;
using MediatR;

namespace CommerceFlow.Application.Carts.Commands.CreateCart;

public sealed class CreateCartCommandHandler(
    ICartRepository carts,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateCartCommand, Guid>
{
    public async Task<Guid> Handle(CreateCartCommand request, CancellationToken cancellationToken)
    {
        if (await carts.GetActiveByCustomerIdAsync(request.CustomerId, cancellationToken) is not null)
            throw new ConflictException($"Customer '{request.CustomerId}' already has an active cart.");

        var cart = Cart.Create(request.CustomerId);

        await carts.AddAsync(cart, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return cart.Id;
    }
}
