using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Commands.PlaceOrder;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;
using Moq;

namespace CommerceFlow.Application.Tests.Orders;

public sealed class PlaceOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private PlaceOrderCommandHandler CreateHandler() => new(_orders.Object, _unitOfWork.Object);

    private static PlaceOrderCommand Command(
        Guid? customerId = null,
        IReadOnlyList<PlaceOrderLineDto>? lines = null,
        PlaceOrderAddressDto? address = null,
        string currency = "USD",
        string? customerNote = null,
        decimal shippingFee = 0,
        decimal discountAmount = 0) =>
        new(
            customerId ?? OrderFixtures.CustomerId,
            lines ?? new List<PlaceOrderLineDto>
            {
                new(Guid.NewGuid(), "SKU-1", "iPhone 15", 100m, 2),
            },
            address ?? new PlaceOrderAddressDto(
                "John Doe", "1 Main Street", "Cairo", "12345", "EG"),
            currency,
            customerNote,
            shippingFee,
            discountAmount);

    [Fact]
    public async Task PlaceOrder_CreatesOrderWithLinesAddressFeeAndDiscount()
    {
        var handler = CreateHandler();
        var command = new PlaceOrderCommand(
            CustomerId: OrderFixtures.CustomerId,
            Lines: new List<PlaceOrderLineDto>
            {
                new(Guid.NewGuid(), "SKU-1", "iPhone 15", 100m, 2), // 200
                new(Guid.NewGuid(), "SKU-2", "AirPods", 50m, 3),    // 150
            },
            ShippingAddress: new PlaceOrderAddressDto(
                "John Doe", "1 Main Street", "Cairo", "12345", "EG",
                Line2: "Apt 4B", State: "CA", PhoneNumber: "01000000000"),
            Currency: "usd",
            CustomerNote: "Leave at the door",
            ShippingFee: 25m,
            DiscountAmount: 50m);

        var id = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _orders.Verify(
            x => x.AddAsync(
                It.Is<Order>(o => o.CustomerId == OrderFixtures.CustomerId
                    && o.Currency == "USD"
                    && o.Status == OrderStatus.Pending
                    && o.PaymentStatus == OrderPaymentStatus.Unpaid
                    && o.Subtotal == 350m
                    && o.ShippingFee == 25m
                    && o.DiscountAmount == 50m
                    && o.GrandTotal == 325m
                    && o.Lines.Count == 2
                    && o.CustomerNote == "Leave at the door"
                    && o.ShippingAddress.FullName == "John Doe"
                    && o.ShippingAddress.Line2 == "Apt 4B"
                    && o.ShippingAddress.State == "CA"
                    && o.ShippingAddress.PhoneNumber == "01000000000"
                    && o.OrderNumber.StartsWith("ORD-")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlaceOrder_WithZeroFeeAndDiscount_LeavesTotalsAtDefaults()
    {
        var handler = CreateHandler();

        await handler.Handle(Command(), CancellationToken.None);

        _orders.Verify(
            x => x.AddAsync(
                It.Is<Order>(o => o.ShippingFee == 0
                    && o.DiscountAmount == 0
                    && o.GrandTotal == 200m),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlaceOrder_WithInvalidAddress_ThrowsDomainExceptionAndDoesNotSave()
    {
        var handler = CreateHandler();
        var command = Command(address: new PlaceOrderAddressDto(
            FullName: "", Line1: "1 Main Street", City: "Cairo", PostalCode: "12345", Country: "EG"));

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));

        _orders.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PlaceOrder_WithInvalidCurrency_ThrowsDomainException()
    {
        var handler = CreateHandler();

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(Command(currency: "DOLLARS"), CancellationToken.None));

        _orders.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PlaceOrder_WithEmptyCustomerId_ThrowsDomainException()
    {
        var handler = CreateHandler();

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(Command(customerId: Guid.Empty), CancellationToken.None));

        _orders.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PlaceOrder_WithDiscountAboveTotal_ThrowsDomainException()
    {
        var handler = CreateHandler();
        var command = Command(discountAmount: 500m); // subtotal 200 < 500

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("Discount cannot exceed the order total", exception.Message);
        _orders.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PlaceOrder_WithInvalidLineQuantity_ThrowsDomainException()
    {
        var handler = CreateHandler();
        var command = Command(lines: new List<PlaceOrderLineDto>
        {
            new(Guid.NewGuid(), "SKU-1", "iPhone 15", 100m, 0),
        });

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
