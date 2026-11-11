using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Tests.Carts;

// Shared factories for building Cart instances in different lifecycle states.
internal static class CartFixtures
{
    public static readonly Guid CustomerId = Guid.Parse("22221111-2222-2222-2222-222222222222");

    public static Cart CreateCart(
        CartStatus status = CartStatus.Active,
        int lines = 0,
        Guid? customerId = null)
    {
        var cart = Cart.Create(customerId ?? CustomerId);

        // A checked-out cart must contain at least one line (domain rule),
        // so bump the requested line count for that state.
        if (status == CartStatus.CheckedOut && lines == 0)
            lines = 1;

        for (var i = 0; i < lines; i++)
            cart.AddItem(Guid.NewGuid(), 1);

        switch (status)
        {
            case CartStatus.CheckedOut:
                cart.MarkCheckedOut();
                break;
            case CartStatus.Abandoned:
                cart.MarkAbandoned();
                break;
        }

        return cart;
    }

    // Active cart with a single line whose id is known, for line-level command tests.
    public static (Cart Cart, CartLine Line) ActiveCartWithLine(
        Guid? variantId = null,
        int quantity = 2)
    {
        var cart = Cart.Create(CustomerId);
        var line = cart.AddItem(variantId ?? Guid.NewGuid(), quantity);
        return (cart, line);
    }

    // Active cart already at MaxLines (100), with one known variant,
    // for the line-limit pre-check tests.
    public static (Cart Cart, Guid VariantId) ActiveCartAtMaxLines()
    {
        var cart = Cart.Create(CustomerId);
        var variantId = Guid.NewGuid();
        cart.AddItem(variantId, 1);

        for (var i = 1; i < Cart.MaxLines; i++)
            cart.AddItem(Guid.NewGuid(), 1);

        return (cart, variantId);
    }
}
