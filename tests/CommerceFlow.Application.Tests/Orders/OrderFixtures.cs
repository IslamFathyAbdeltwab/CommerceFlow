using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Tests.Orders;

// Shared factories for building Order instances in different lifecycle states.
internal static class OrderFixtures
{
    public static readonly Guid CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static AddressSnapshot CreateAddress(
        string fullName = "John Doe",
        string line1 = "1 Main Street",
        string city = "Cairo",
        string postalCode = "12345",
        string country = "EG",
        string? line2 = "Apt 4B",
        string? state = "CA",
        string? phoneNumber = "01000000000") =>
        AddressSnapshot.Create(fullName, line1, city, postalCode, country, line2, state, phoneNumber);

    public static Order CreateOrder(
        OrderStatus status = OrderStatus.Pending,
        int lineCount = 1,
        decimal unitPrice = 100m,
        int quantity = 2)
    {
        var order = Order.Create(CustomerId, CreateAddress(), "USD", "Leave at the door.");

        for (var i = 0; i < lineCount; i++)
            order.AddLine(Guid.NewGuid(), $"SKU-{i + 1}", $"Product {i + 1}", unitPrice, quantity);

        switch (status)
        {
            case OrderStatus.Pending:
                break;
            case OrderStatus.Confirmed:
                order.Confirm();
                break;
            case OrderStatus.Shipped:
                order.Confirm();
                order.Ship();
                break;
            case OrderStatus.Delivered:
                order.Confirm();
                order.Ship();
                order.Deliver();
                break;
            case OrderStatus.Cancelled:
                order.Cancel("Customer changed their mind.");
                break;
        }

        return order;
    }

    // Paid orders must be paid while still pending or confirmed (domain rule),
    // so the cancelled-but-paid case pays first and cancels afterwards.
    public static Order CreatePaidOrder(OrderStatus status = OrderStatus.Pending)
    {
        var order = CreateOrder(status);
        order.MarkAsPaid();
        return order;
    }

    public static Order CreateCancelledPaidOrder()
    {
        var order = CreateOrder();
        order.MarkAsPaid();
        order.Cancel("Customer changed their mind.");
        return order;
    }
}
