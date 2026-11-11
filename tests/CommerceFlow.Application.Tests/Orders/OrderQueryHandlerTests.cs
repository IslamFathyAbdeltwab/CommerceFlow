using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Application.Orders.Queries.GetOrderById;
using CommerceFlow.Application.Orders.Queries.GetOrders;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Orders;

public sealed class OrderQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orders = new();

    [Fact]
    public async Task GetOrderById_WhenOrderDoesNotExist_ReturnsNull()
    {
        var handler = new GetOrderByIdQueryHandler(_orders.Object);
        _orders
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await handler.Handle(new GetOrderByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrderById_WhenPendingOrder_MapsAddressLinesAndTotals()
    {
        var handler = new GetOrderByIdQueryHandler(_orders.Object);
        var order = OrderFixtures.CreateOrder(); // 100 x 2 = 200
        order.SetShippingFee(25m);
        order.ApplyDiscount(50m);
        _orders
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal(order.OrderNumber, result.OrderNumber);
        Assert.StartsWith("ORD-", result.OrderNumber);
        Assert.Equal(OrderFixtures.CustomerId, result.CustomerId);
        Assert.Equal(OrderStatus.Pending, result.Status);
        Assert.Equal(OrderPaymentStatus.Unpaid, result.PaymentStatus);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(200m, result.Subtotal);
        Assert.Equal(25m, result.ShippingFee);
        Assert.Equal(50m, result.DiscountAmount);
        Assert.Equal(175m, result.GrandTotal);
        Assert.Equal("Leave at the door.", result.CustomerNote);
        Assert.Null(result.CancelReason);
        Assert.Null(result.PaidAtUtc);
        Assert.Null(result.ShippedAtUtc);
        Assert.Null(result.DeliveredAtUtc);
        Assert.Null(result.CancelledAtUtc);
        Assert.Null(result.RefundedAtUtc);

        Assert.Equal("John Doe", result.ShippingAddress.FullName);
        Assert.Equal("1 Main Street", result.ShippingAddress.Line1);
        Assert.Equal("Apt 4B", result.ShippingAddress.Line2);
        Assert.Equal("Cairo", result.ShippingAddress.City);
        Assert.Equal("CA", result.ShippingAddress.State);
        Assert.Equal("12345", result.ShippingAddress.PostalCode);
        Assert.Equal("EG", result.ShippingAddress.Country);
        Assert.Equal("01000000000", result.ShippingAddress.PhoneNumber);

        var line = Assert.Single(result.Lines);
        Assert.Equal("SKU-1", line.Sku);
        Assert.Equal("Product 1", line.ProductName);
        Assert.Equal(100m, line.UnitPrice);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(200m, line.LineTotal);
        Assert.Equal(order.Lines.Single().Id, line.Id);
    }

    [Fact]
    public async Task GetOrderById_WhenConfirmedAndPaid_MapsPaymentTimestamp()
    {
        var handler = new GetOrderByIdQueryHandler(_orders.Object);
        var order = OrderFixtures.CreatePaidOrder(OrderStatus.Confirmed);
        _orders
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(OrderStatus.Confirmed, result.Status);
        Assert.Equal(OrderPaymentStatus.Paid, result.PaymentStatus);
        Assert.NotNull(result.PaidAtUtc);
        Assert.Null(result.ShippedAtUtc);
    }

    [Fact]
    public async Task GetOrderById_WhenCancelled_MapsCancelReasonAndTimestamp()
    {
        var handler = new GetOrderByIdQueryHandler(_orders.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Cancelled);
        _orders
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(OrderStatus.Cancelled, result.Status);
        Assert.Equal("Customer changed their mind.", result.CancelReason);
        Assert.NotNull(result.CancelledAtUtc);
        Assert.Null(result.RefundedAtUtc);
    }

    [Fact]
    public async Task GetOrders_ReturnsPagedResultWithFiltersPassedThrough()
    {
        var handler = new GetOrdersQueryHandler(_orders.Object);
        var customerId = Guid.NewGuid();
        var first = OrderFixtures.CreateOrder();
        var second = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        _orders
            .Setup(x => x.CountAsync(
                customerId, OrderStatus.Pending, OrderPaymentStatus.Unpaid, "ORD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        _orders
            .Setup(x => x.GetAllAsync(
                customerId, OrderStatus.Pending, OrderPaymentStatus.Unpaid, "ORD", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { first, second });

        var result = await handler.Handle(
            new GetOrdersQuery(
                CustomerId: customerId,
                Status: OrderStatus.Pending,
                PaymentStatus: OrderPaymentStatus.Unpaid,
                SearchTerm: "ORD",
                PageNumber: 2,
                PageSize: 10),
            CancellationToken.None);

        Assert.Equal(42, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(5, result.TotalPages);
        Assert.Equal(2, result.Items.Count);

        Assert.Equal(first.Id, result.Items[0].Id);
        Assert.Equal(first.OrderNumber, result.Items[0].OrderNumber);
        Assert.Equal(OrderFixtures.CustomerId, result.Items[0].CustomerId);
        Assert.Equal(OrderStatus.Pending, result.Items[0].Status);
        Assert.Equal(OrderPaymentStatus.Unpaid, result.Items[0].PaymentStatus);
        Assert.Equal("USD", result.Items[0].Currency);
        Assert.Equal(200m, result.Items[0].GrandTotal);
        Assert.Equal(first.CreatedAtUtc, result.Items[0].CreatedAtUtc);
        Assert.Equal(OrderStatus.Confirmed, result.Items[1].Status);

        _orders.Verify(
            x => x.CountAsync(
                customerId, OrderStatus.Pending, OrderPaymentStatus.Unpaid, "ORD", It.IsAny<CancellationToken>()),
            Times.Once);
        _orders.Verify(
            x => x.GetAllAsync(
                customerId, OrderStatus.Pending, OrderPaymentStatus.Unpaid, "ORD", 2, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrders_WithNoFilters_PassesNullsThrough()
    {
        var handler = new GetOrdersQueryHandler(_orders.Object);
        _orders
            .Setup(x => x.CountAsync(null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _orders
            .Setup(x => x.GetAllAsync(null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var result = await handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }
}
