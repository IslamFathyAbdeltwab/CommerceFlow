using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Orders;

public class OrderTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static AddressSnapshot NewAddress() =>
        AddressSnapshot.Create("John Doe", "123 Main", "Alex", "21500", "Egypt");

    private static Order NewOrder() => Order.Create(CustomerId, NewAddress());

    private static Order NewOrderWithLine(decimal unitPrice = 100m, int quantity = 2)
    {
        var order = NewOrder();
        order.AddLine(Guid.NewGuid(), "SKU-1", "iPhone 15", unitPrice, quantity);
        return order;
    }

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_StartsPendingUnpaidWithDefaults()
    {
        var order = NewOrder();

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(OrderPaymentStatus.Unpaid, order.PaymentStatus);
        Assert.Equal("USD", order.Currency);
        Assert.Equal(CustomerId, order.CustomerId);
        Assert.Equal(0m, order.Subtotal);
        Assert.Equal(0m, order.ShippingFee);
        Assert.Equal(0m, order.DiscountAmount);
        Assert.Equal(0m, order.GrandTotal);
        Assert.Null(order.CustomerNote);
        Assert.Null(order.CancelReason);
        Assert.Null(order.PaidAtUtc);
        Assert.Null(order.ShippedAtUtc);
        Assert.Null(order.DeliveredAtUtc);
        Assert.Null(order.CancelledAtUtc);
        Assert.Null(order.RefundedAtUtc);
        Assert.Empty(order.Lines);
        Assert.NotEqual(default(DateTimeOffset), order.CreatedAtUtc);
    }

    [Fact]
    public void Create_OrderNumber_StartsWithPrefix()
    {
        var order = NewOrder();
        Assert.StartsWith("ORD-", order.OrderNumber);
        Assert.True(order.OrderNumber.Length > 4);
    }

    [Fact]
    public void GenerateOrderNumber_TwoCalls_ProduceDifferentNumbers()
    {
        Assert.NotEqual(Order.GenerateOrderNumber(), Order.GenerateOrderNumber());
    }

    [Fact]
    public void Create_EmptyCustomerId_Throws()
    {
        Assert.Throws<DomainException>(() => Order.Create(Guid.Empty, NewAddress()));
    }

    [Fact]
    public void Create_NullAddress_Throws()
    {
        Assert.Throws<DomainException>(() => Order.Create(CustomerId, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("U5D")]
    [InlineData("   ")]
    public void Create_InvalidCurrency_Throws(string currency)
    {
        Assert.Throws<DomainException>(() => Order.Create(CustomerId, NewAddress(), currency));
    }

    [Fact]
    public void Create_Currency_IsTrimmedAndUppercased()
    {
        var order = Order.Create(CustomerId, NewAddress(), " egd ");
        Assert.Equal("EGD", order.Currency);
    }

    [Fact]
    public void Create_BlankCustomerNote_IsStoredAsNull()
    {
        var order = Order.Create(CustomerId, NewAddress(), customerNote: "   ");
        Assert.Null(order.CustomerNote);
    }

    [Fact]
    public void Create_CustomerNote_IsTrimmed()
    {
        var order = Order.Create(CustomerId, NewAddress(), customerNote: " Leave at the door ");
        Assert.Equal("Leave at the door", order.CustomerNote);
    }

    [Fact]
    public void Create_CustomerNoteLongerThanMaximum_Throws()
    {
        var tooLong = new string('n', Order.CustomerNoteMaxLength + 1);
        Assert.Throws<DomainException>(() => Order.Create(CustomerId, NewAddress(), customerNote: tooLong));
    }

    // ---------- Totals ----------

    [Fact]
    public void GrandTotal_IsSubtotalPlusShippingMinusDiscount()
    {
        var order = NewOrderWithLine(100m, 2); // subtotal 200
        order.SetShippingFee(25m);
        order.ApplyDiscount(50m);

        Assert.Equal(200m, order.Subtotal);
        Assert.Equal(25m, order.ShippingFee);
        Assert.Equal(50m, order.DiscountAmount);
        Assert.Equal(175m, order.GrandTotal);
    }

    [Fact]
    public void SetShippingFee_Valid_UpdatesGrandTotal()
    {
        var order = NewOrderWithLine(100m, 1);

        order.SetShippingFee(30m);

        Assert.Equal(30m, order.ShippingFee);
        Assert.Equal(130m, order.GrandTotal);
    }

    [Fact]
    public void SetShippingFee_Negative_Throws()
    {
        var order = NewOrderWithLine();
        Assert.Throws<DomainException>(() => order.SetShippingFee(-1m));
    }

    [Fact]
    public void ApplyDiscount_Valid_ReducesGrandTotal()
    {
        var order = NewOrderWithLine(100m, 2); // 200

        order.ApplyDiscount(40m);

        Assert.Equal(40m, order.DiscountAmount);
        Assert.Equal(160m, order.GrandTotal);
    }

    [Fact]
    public void ApplyDiscount_Negative_Throws()
    {
        var order = NewOrderWithLine();
        Assert.Throws<DomainException>(() => order.ApplyDiscount(-5m));
    }

    [Fact]
    public void ApplyDiscount_ExceedingOrderTotal_Throws()
    {
        var order = NewOrderWithLine(100m, 1); // 100

        Assert.Throws<DomainException>(() => order.ApplyDiscount(101m));
        Assert.Equal(0m, order.DiscountAmount);
    }

    [Fact]
    public void ApplyDiscount_EqualToOrderTotal_IsAllowedAndGrandTotalBecomesZero()
    {
        var order = NewOrderWithLine(100m, 1);

        order.ApplyDiscount(100m);

        Assert.Equal(0m, order.GrandTotal);
    }

    [Fact]
    public void RemoveLine_WhenDiscountWouldExceedNewTotal_ThrowsAndKeepsState()
    {
        var order = NewOrder();
        var small = order.AddLine(Guid.NewGuid(), "SKU-1", "Case", 50m, 1);
        order.AddLine(Guid.NewGuid(), "SKU-2", "iPhone 15", 100m, 1); // subtotal 150
        order.ApplyDiscount(120m); // valid against 150

        Assert.Throws<DomainException>(() => order.RemoveLine(small.Id));

        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(150m, order.Subtotal);
        Assert.Equal(120m, order.DiscountAmount);
    }

    // ---------- Editing lock ----------

    [Fact]
    public void EditOperations_AfterConfirm_Throw()
    {
        var order = NewOrderWithLine();
        var line = order.Lines.Single();
        order.Confirm();

        Assert.Throws<DomainException>(() =>
            order.AddLine(Guid.NewGuid(), "SKU-2", "Case", 50m, 1));
        Assert.Throws<DomainException>(() => order.UpdateLineQuantity(line.Id, 3));
        Assert.Throws<DomainException>(() => order.RemoveLine(line.Id));
        Assert.Throws<DomainException>(() => order.SetShippingFee(10m));
        Assert.Throws<DomainException>(() => order.ApplyDiscount(10m));
    }

    [Fact]
    public void AddLine_BeyondMaxLines_Throws()
    {
        var order = NewOrder();
        for (var i = 0; i < Order.MaxLines; i++)
            order.AddLine(Guid.NewGuid(), $"SKU-{i}", $"Product {i}", 10m, 1);

        Assert.Equal(Order.MaxLines, order.Lines.Count);
        Assert.Throws<DomainException>(() =>
            order.AddLine(Guid.NewGuid(), "SKU-extra", "Extra", 10m, 1));
    }

    // ---------- Fulfillment state machine ----------

    [Fact]
    public void Confirm_WithoutLines_Throws()
    {
        var order = NewOrder();
        Assert.Throws<DomainException>(() => order.Confirm());
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void Confirm_FromPending_BecomesConfirmed()
    {
        var order = NewOrderWithLine();

        order.Confirm();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_Twice_Throws()
    {
        var order = NewOrderWithLine();
        order.Confirm();

        Assert.Throws<DomainException>(() => order.Confirm());
    }

    [Fact]
    public void Ship_FromPending_Throws()
    {
        var order = NewOrderWithLine();
        Assert.Throws<DomainException>(() => order.Ship());
    }

    [Fact]
    public void Ship_FromConfirmed_BecomesShippedWithTimestamp()
    {
        var order = NewOrderWithLine();
        order.Confirm();

        order.Ship();

        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.NotNull(order.ShippedAtUtc);
    }

    [Fact]
    public void Deliver_FromConfirmed_Throws()
    {
        var order = NewOrderWithLine();
        order.Confirm();

        Assert.Throws<DomainException>(() => order.Deliver());
    }

    [Fact]
    public void Deliver_FromShipped_BecomesDeliveredWithTimestamp()
    {
        var order = NewOrderWithLine();
        order.Confirm();
        order.Ship();

        order.Deliver();

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.NotNull(order.DeliveredAtUtc);
    }

    [Fact]
    public void Cancel_FromPending_StoresReasonAndTimestamp()
    {
        var order = NewOrderWithLine();

        order.Cancel(" Customer changed mind ");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Customer changed mind", order.CancelReason);
        Assert.NotNull(order.CancelledAtUtc);
    }

    [Fact]
    public void Cancel_BlankReason_IsStoredAsNull()
    {
        var order = NewOrderWithLine();

        order.Cancel("   ");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Null(order.CancelReason);
    }

    [Fact]
    public void Cancel_ReasonLongerThanMaximum_Throws()
    {
        var order = NewOrderWithLine();
        var tooLong = new string('r', Order.CancelReasonMaxLength + 1);

        Assert.Throws<DomainException>(() => order.Cancel(tooLong));
    }

    [Fact]
    public void Cancel_FromConfirmed_IsAllowed()
    {
        var order = NewOrderWithLine();
        order.Confirm();

        order.Cancel("Out of stock");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_FromShipped_Throws()
    {
        var order = NewOrderWithLine();
        order.Confirm();
        order.Ship();

        Assert.Throws<DomainException>(() => order.Cancel());
    }

    [Fact]
    public void Cancel_FromDelivered_Throws()
    {
        var order = NewOrderWithLine();
        order.Confirm();
        order.Ship();
        order.Deliver();

        Assert.Throws<DomainException>(() => order.Cancel());
    }

    [Fact]
    public void FullLifecycle_ConfirmShipDeliver_TimestampsAreSetInOrder()
    {
        var order = NewOrderWithLine();
        order.Confirm();
        order.MarkAsPaid();
        order.Ship();
        order.Deliver();

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.NotNull(order.PaidAtUtc);
        Assert.NotNull(order.ShippedAtUtc);
        Assert.NotNull(order.DeliveredAtUtc);
        Assert.True(order.PaidAtUtc <= order.ShippedAtUtc);
        Assert.True(order.ShippedAtUtc <= order.DeliveredAtUtc);
    }

    // ---------- Payment lifecycle (independent) ----------

    [Fact]
    public void MarkAsPaid_FromPending_BecomesPaidWithTimestamp()
    {
        var order = NewOrderWithLine();

        order.MarkAsPaid();

        Assert.Equal(OrderPaymentStatus.Paid, order.PaymentStatus);
        Assert.NotNull(order.PaidAtUtc);
        Assert.Equal(OrderStatus.Pending, order.Status); // fulfillment untouched
    }

    [Fact]
    public void MarkAsPaid_Twice_Throws()
    {
        var order = NewOrderWithLine();
        order.MarkAsPaid();

        Assert.Throws<DomainException>(() => order.MarkAsPaid());
    }

    [Fact]
    public void MarkAsPaid_AfterShip_Throws()
    {
        var order = NewOrderWithLine();
        order.Confirm();
        order.Ship();

        Assert.Throws<DomainException>(() => order.MarkAsPaid());
    }

    [Fact]
    public void Refund_WhenUnpaid_Throws()
    {
        var order = NewOrderWithLine();
        Assert.Throws<DomainException>(() => order.Refund());
    }

    [Fact]
    public void Refund_WhenPaid_RefundsWithTimestamp()
    {
        var order = NewOrderWithLine();
        order.MarkAsPaid();

        order.Refund();

        Assert.Equal(OrderPaymentStatus.Refunded, order.PaymentStatus);
        Assert.NotNull(order.RefundedAtUtc);
    }

    [Fact]
    public void Refund_Twice_Throws()
    {
        var order = NewOrderWithLine();
        order.MarkAsPaid();
        order.Refund();

        Assert.Throws<DomainException>(() => order.Refund());
    }

    [Fact]
    public void PaidOrderCancelled_ThenRefunded_KeepsFulfilmentStatusSeparate()
    {
        var order = NewOrderWithLine();
        order.Confirm();
        order.MarkAsPaid();

        order.Cancel("Fraud suspicion");
        order.Refund();

        // Payment refunded but fulfilment history is preserved - the two
        // lifecycles must not overwrite each other.
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(OrderPaymentStatus.Refunded, order.PaymentStatus);
        Assert.NotNull(order.CancelledAtUtc);
        Assert.NotNull(order.RefundedAtUtc);
    }

    [Fact]
    public void Ship_DoesNotChangePaymentStatus()
    {
        var order = NewOrderWithLine();
        order.Confirm();

        order.Ship();

        Assert.Equal(OrderPaymentStatus.Unpaid, order.PaymentStatus);
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }
}
