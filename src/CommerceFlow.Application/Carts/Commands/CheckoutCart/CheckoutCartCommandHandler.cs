using CommerceFlow.Application.Carts.Interfaces;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Carts.Commands.CheckoutCart;

// Checkout is the one flow that spans modules: a cart (Carts) becomes an
// order (Orders) at prices read from the catalog right now - cart lines
// carry no money on purpose (see CartLine). Cross-module reads go through
// the ports (repositories), never through another module's aggregates, and
// everything commits in ONE SaveChanges: the new order and the converted
// cart land together, so there is no window where one exists without the
// other.
public sealed class CheckoutCartCommandHandler(
    ICartRepository carts,
    IProductRepository products,
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<CheckoutCartCommand, Guid>
{
    public async Task<Guid> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await carts.GetByIdAsync(request.CartId, cancellationToken)
            ?? throw new NotFoundException(nameof(Cart), request.CartId);

        if (cart.Status != CartStatus.Active)
            throw new ConflictException($"Only active carts can be checked out (current: {cart.Status}).");

        if (cart.Lines.Count == 0)
            throw new ConflictException("Cannot check out an empty cart.");

        // snapshot the catalog now: the price on the order is the price the
        // customer saw at checkout, whatever the catalog does afterwards
        var variantIds = cart.Lines.Select(l => l.ProductVariantId).Distinct().ToList();
        var snapshots = await products.GetVariantSnapshotsAsync(variantIds, cancellationToken);

        // a variant deleted since it was added cannot be priced - refuse
        // instead of guessing (the cart stays active for the customer)
        var missing = variantIds
            .Where(id => snapshots.All(s => s.ProductVariantId != id))
            .ToList();
        if (missing.Count > 0)
            throw new ConflictException(
                $"Cannot check out: {missing.Count} product variant(s) no longer exist.");

        var address = AddressSnapshot.Create(
            request.ShippingAddress.FullName,
            request.ShippingAddress.Line1,
            request.ShippingAddress.City,
            request.ShippingAddress.PostalCode,
            request.ShippingAddress.Country,
            request.ShippingAddress.Line2,
            request.ShippingAddress.State,
            request.ShippingAddress.PhoneNumber);

        // single-currency catalog: the cart carries no currency, so checkout
        // uses the same default as PlaceOrder
        var order = Order.Create(cart.CustomerId, address, "USD", request.CustomerNote);

        foreach (var line in cart.Lines)
        {
            var snapshot = snapshots.Single(s => s.ProductVariantId == line.ProductVariantId);
            order.AddLine(
                line.ProductVariantId,
                snapshot.Sku,
                snapshot.ProductName,
                snapshot.UnitPrice,
                line.Quantity);
        }

        await orders.AddAsync(order, cancellationToken);

        cart.MarkCheckedOut(); // the domain repeats active + non-empty as its safety net

        await unitOfWork.SaveChangesAsync(cancellationToken); // one commit for both

        return order.Id;
    }
}
