using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Shipping;

public class ShipmentTests
{
    private static readonly Guid OrderId = Guid.NewGuid();

    private static Shipment NewShipment(
        string carrier = "DHL",
        string trackingNumber = "JD0002223334445",
        string? serviceLevel = null) =>
        Shipment.Create(OrderId, carrier, trackingNumber, serviceLevel);

    // MarkShipped refuses an empty box, so state helpers pack an item first.
    private static Shipment ShippedShipment()
    {
        var shipment = NewShipment();
        shipment.AddItem(Guid.NewGuid(), quantity: 2);
        shipment.MarkShipped();
        return shipment;
    }

    private static Shipment InTransitShipment()
    {
        var shipment = ShippedShipment();
        shipment.MarkInTransit();
        return shipment;
    }

    private static Shipment DeliveredShipment()
    {
        var shipment = InTransitShipment();
        shipment.MarkDelivered();
        return shipment;
    }

    private static Shipment FailedShipment()
    {
        var shipment = ShippedShipment();
        shipment.MarkFailed("Address unknown");
        return shipment;
    }

    private static Shipment ReturnedShipment()
    {
        var shipment = FailedShipment();
        shipment.ReturnToSender();
        return shipment;
    }

    private static Shipment CancelledShipment()
    {
        var shipment = NewShipment();
        shipment.Cancel();
        return shipment;
    }

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_CreatesPendingShipmentForOrder()
    {
        var shipment = NewShipment();

        Assert.Equal(OrderId, shipment.OrderId);
        Assert.Equal(ShipmentStatus.Pending, shipment.Status);
        Assert.Equal("DHL", shipment.Carrier);
        Assert.Equal("JD0002223334445", shipment.TrackingNumber);
        Assert.Null(shipment.ServiceLevel);
        Assert.Null(shipment.ShippedAtUtc);
        Assert.Null(shipment.DeliveredAtUtc);
        Assert.Null(shipment.CancelledAtUtc);
        Assert.Null(shipment.FailureReason);
        Assert.Empty(shipment.Items);
        Assert.NotEqual(Guid.Empty, shipment.Id);
    }

    [Fact]
    public void Create_SetsAuditTimestamps()
    {
        var shipment = NewShipment();

        Assert.NotEqual(default, shipment.CreatedAtUtc);
        Assert.NotEqual(default, shipment.UpdatedAtUtc);
        Assert.True(shipment.UpdatedAtUtc >= shipment.CreatedAtUtc);
    }

    [Fact]
    public void Create_EmptyOrderId_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Shipment.Create(Guid.Empty, "DHL", "JD0002223334445"));

        Assert.Equal("Order ID is required.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingCarrier_Throws(string carrier)
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewShipment(carrier: carrier));

        Assert.Equal("Carrier is required.", exception.Message);
    }

    [Fact]
    public void Create_CarrierTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewShipment(carrier: new string('c', Shipment.CarrierMaxLength + 1)));

        Assert.Equal("Carrier cannot exceed 100 characters.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingTrackingNumber_Throws(string trackingNumber)
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewShipment(trackingNumber: trackingNumber));

        Assert.Equal("Tracking number is required.", exception.Message);
    }

    [Fact]
    public void Create_TrackingNumberTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewShipment(trackingNumber: new string('t', Shipment.TrackingNumberMaxLength + 1)));

        Assert.Equal("Tracking number cannot exceed 100 characters.", exception.Message);
    }

    [Fact]
    public void Create_TrimsCarrierAndTrackingNumber()
    {
        var shipment = NewShipment(carrier: "  FedEx  ", trackingNumber: "  7712  ");

        Assert.Equal("FedEx", shipment.Carrier);
        Assert.Equal("7712", shipment.TrackingNumber);
    }

    [Fact]
    public void Create_WithServiceLevel_StoresTrimmed()
    {
        var shipment = NewShipment(serviceLevel: "  Express  ");

        Assert.Equal("Express", shipment.ServiceLevel);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_OptionalServiceLevel_EmptyOrNullBecomesNull(string? serviceLevel)
    {
        Assert.Null(NewShipment(serviceLevel: serviceLevel).ServiceLevel);
    }

    [Fact]
    public void Create_ServiceLevelTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewShipment(serviceLevel: new string('s', Shipment.ServiceLevelMaxLength + 1)));

        Assert.Equal("Service level cannot exceed 50 characters.", exception.Message);
    }

    // ---------- AddItem ----------

    [Fact]
    public void AddItem_Valid_AddsItemToBox()
    {
        var shipment = NewShipment();
        var variantId = Guid.NewGuid();

        shipment.AddItem(variantId, quantity: 3);

        var item = Assert.Single(shipment.Items);
        Assert.Equal(variantId, item.ProductVariantId);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(shipment.Id, item.ShipmentId);
        Assert.NotEqual(Guid.Empty, item.Id);
    }

    [Fact]
    public void AddItem_TwoDistinctVariants_BothAllowed()
    {
        var shipment = NewShipment();

        shipment.AddItem(Guid.NewGuid(), 1);
        shipment.AddItem(Guid.NewGuid(), 2);

        Assert.Equal(2, shipment.Items.Count);
    }

    [Fact]
    public void AddItem_EmptyVariantId_Throws()
    {
        var shipment = NewShipment();

        var exception = Assert.Throws<DomainException>(() =>
            shipment.AddItem(Guid.Empty, 1));

        Assert.Equal("Product variant ID is required.", exception.Message);
        Assert.Empty(shipment.Items);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_NonPositiveQuantity_Throws(int quantity)
    {
        var shipment = NewShipment();

        var exception = Assert.Throws<DomainException>(() =>
            shipment.AddItem(Guid.NewGuid(), quantity));

        Assert.Equal("Quantity must be positive.", exception.Message);
        Assert.Empty(shipment.Items);
    }

    [Fact]
    public void AddItem_DuplicateVariant_ThrowsAndKeepsOriginal()
    {
        var shipment = NewShipment();
        var variantId = Guid.NewGuid();
        shipment.AddItem(variantId, 1);

        var exception = Assert.Throws<DomainException>(() =>
            shipment.AddItem(variantId, 5));

        Assert.Equal("Shipment already contains this variant.", exception.Message);
        Assert.Single(shipment.Items);
        Assert.Equal(1, shipment.Items.First().Quantity); // original kept
    }

    [Fact]
    public void AddItem_AfterShipped_ThrowsAndBoxUnchanged()
    {
        var shipment = ShippedShipment();
        var countBefore = shipment.Items.Count;

        var exception = Assert.Throws<DomainException>(() =>
            shipment.AddItem(Guid.NewGuid(), 1));

        Assert.Equal(
            "Shipment items can only be added while the shipment is pending.",
            exception.Message);
        Assert.Equal(countBefore, shipment.Items.Count);
    }

    [Fact]
    public void AddItem_UpToMaxItems_IsAllowed()
    {
        var shipment = NewShipment();

        for (var i = 0; i < Shipment.MaxItems; i++)
            shipment.AddItem(Guid.NewGuid(), 1);

        Assert.Equal(Shipment.MaxItems, shipment.Items.Count);
    }

    [Fact]
    public void AddItem_BeyondMaxItems_Throws()
    {
        var shipment = NewShipment();
        for (var i = 0; i < Shipment.MaxItems; i++)
            shipment.AddItem(Guid.NewGuid(), 1);

        var exception = Assert.Throws<DomainException>(() =>
            shipment.AddItem(Guid.NewGuid(), 1));

        Assert.Equal("Shipment cannot contain more than 100 items.", exception.Message);
        Assert.Equal(Shipment.MaxItems, shipment.Items.Count);
    }

    // ---------- MarkShipped ----------

    [Fact]
    public void MarkShipped_FromPending_ShipsAndSetsTimestamp()
    {
        var shipment = NewShipment();
        shipment.AddItem(Guid.NewGuid(), 1);

        shipment.MarkShipped();

        Assert.Equal(ShipmentStatus.Shipped, shipment.Status);
        Assert.NotNull(shipment.ShippedAtUtc);
        Assert.True(shipment.ShippedAtUtc >= shipment.CreatedAtUtc);
        Assert.Null(shipment.DeliveredAtUtc);
        Assert.Null(shipment.CancelledAtUtc);
    }

    [Fact]
    public void MarkShipped_WithoutItems_ThrowsAndStaysPending()
    {
        var shipment = NewShipment();

        var exception = Assert.Throws<DomainException>(() => shipment.MarkShipped());

        Assert.Equal(
            "Shipment must contain at least one item before it can be shipped.",
            exception.Message);
        Assert.Equal(ShipmentStatus.Pending, shipment.Status);
        Assert.Null(shipment.ShippedAtUtc);
    }

    [Fact]
    public void MarkShipped_WhenAlreadyShipped_IsNoOp()
    {
        var shipment = ShippedShipment();
        var shippedAt = shipment.ShippedAtUtc;
        var updatedAt = shipment.UpdatedAtUtc;

        shipment.MarkShipped();

        Assert.Equal(ShipmentStatus.Shipped, shipment.Status);
        Assert.Equal(shippedAt, shipment.ShippedAtUtc);
        Assert.Equal(updatedAt, shipment.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void MarkShipped_FromInTransit_Throws()
    {
        var shipment = InTransitShipment();

        var exception = Assert.Throws<DomainException>(() => shipment.MarkShipped());

        Assert.Equal("Shipment cannot move from InTransit to Shipped.", exception.Message);
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
    }

    [Fact]
    public void MarkShipped_FromCancelled_Throws()
    {
        var shipment = CancelledShipment();

        var exception = Assert.Throws<DomainException>(() => shipment.MarkShipped());

        Assert.Equal("Shipment cannot move from Cancelled to Shipped.", exception.Message);
        Assert.Equal(ShipmentStatus.Cancelled, shipment.Status);
    }

    [Fact]
    public void MarkShipped_FromDelivered_ThrowsAndKeepsDelivery()
    {
        var shipment = DeliveredShipment();
        var deliveredAt = shipment.DeliveredAtUtc;

        var exception = Assert.Throws<DomainException>(() => shipment.MarkShipped());

        Assert.Equal("Shipment cannot move from Delivered to Shipped.", exception.Message);
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Equal(deliveredAt, shipment.DeliveredAtUtc);
    }

    // ---------- MarkInTransit ----------

    [Fact]
    public void MarkInTransit_FromShipped_MovesAndKeepsShippedTimestamp()
    {
        var shipment = ShippedShipment();
        var shippedAt = shipment.ShippedAtUtc;

        shipment.MarkInTransit();

        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        Assert.Equal(shippedAt, shipment.ShippedAtUtc); // untouched
        Assert.True(shipment.UpdatedAtUtc >= shippedAt);
    }

    [Fact]
    public void MarkInTransit_WhenAlreadyInTransit_IsNoOp()
    {
        var shipment = InTransitShipment();
        var updatedAt = shipment.UpdatedAtUtc;

        shipment.MarkInTransit();

        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        Assert.Equal(updatedAt, shipment.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void MarkInTransit_FromPending_ThrowsAndStaysPending()
    {
        var shipment = NewShipment();
        shipment.AddItem(Guid.NewGuid(), 1);

        var exception = Assert.Throws<DomainException>(() => shipment.MarkInTransit());

        Assert.Equal("Shipment cannot move from Pending to InTransit.", exception.Message);
        Assert.Equal(ShipmentStatus.Pending, shipment.Status);
    }

    [Fact]
    public void MarkInTransit_FromDelivered_Throws()
    {
        var shipment = DeliveredShipment();

        var exception = Assert.Throws<DomainException>(() => shipment.MarkInTransit());

        Assert.Equal("Shipment cannot move from Delivered to InTransit.", exception.Message);
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
    }

    // ---------- MarkDelivered ----------

    [Fact]
    public void MarkDelivered_FromInTransit_DeliversAndSetsTimestamp()
    {
        var shipment = InTransitShipment();
        var shippedAt = shipment.ShippedAtUtc;

        shipment.MarkDelivered();

        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.NotNull(shipment.DeliveredAtUtc);
        Assert.True(shipment.DeliveredAtUtc >= shippedAt);
    }

    [Fact]
    public void MarkDelivered_FromShipped_SkipsTransitScan()
    {
        // some carriers scan Delivered without an intermediate InTransit scan
        var shipment = ShippedShipment();

        shipment.MarkDelivered();

        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.NotNull(shipment.DeliveredAtUtc);
    }

    [Fact]
    public void MarkDelivered_WhenAlreadyDelivered_IsNoOp()
    {
        var shipment = DeliveredShipment();
        var deliveredAt = shipment.DeliveredAtUtc;
        var updatedAt = shipment.UpdatedAtUtc;

        shipment.MarkDelivered();

        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Equal(deliveredAt, shipment.DeliveredAtUtc);
        Assert.Equal(updatedAt, shipment.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void MarkDelivered_FromPending_ThrowsAndStaysPending()
    {
        var shipment = NewShipment();
        shipment.AddItem(Guid.NewGuid(), 1);

        var exception = Assert.Throws<DomainException>(() => shipment.MarkDelivered());

        Assert.Equal("Shipment cannot move from Pending to Delivered.", exception.Message);
        Assert.Equal(ShipmentStatus.Pending, shipment.Status);
        Assert.Null(shipment.DeliveredAtUtc);
    }

    [Fact]
    public void MarkDelivered_FromFailed_ThrowsAndKeepsFailure()
    {
        var shipment = FailedShipment();

        var exception = Assert.Throws<DomainException>(() => shipment.MarkDelivered());

        Assert.Equal("Shipment cannot move from Failed to Delivered.", exception.Message);
        Assert.Equal(ShipmentStatus.Failed, shipment.Status);
        Assert.Equal("Address unknown", shipment.FailureReason);
    }

    [Fact]
    public void MarkDelivered_FromCancelled_Throws()
    {
        var shipment = CancelledShipment();

        var exception = Assert.Throws<DomainException>(() => shipment.MarkDelivered());

        Assert.Equal("Shipment cannot move from Cancelled to Delivered.", exception.Message);
        Assert.Equal(ShipmentStatus.Cancelled, shipment.Status);
    }

    // ---------- MarkFailed ----------

    [Theory]
    [InlineData(ShipmentStatus.Shipped)]
    [InlineData(ShipmentStatus.InTransit)]
    public void MarkFailed_FromCarrierStates_FailsAndStoresReason(ShipmentStatus from)
    {
        var shipment = NewShipment();
        shipment.AddItem(Guid.NewGuid(), 1);
        shipment.MarkShipped();
        if (from == ShipmentStatus.InTransit)
            shipment.MarkInTransit();

        shipment.MarkFailed("  Wrong address  ");

        Assert.Equal(ShipmentStatus.Failed, shipment.Status);
        Assert.Equal("Wrong address", shipment.FailureReason); // trimmed
        Assert.Null(shipment.DeliveredAtUtc);
        Assert.Null(shipment.CancelledAtUtc);
    }

    [Fact]
    public void MarkFailed_MissingReason_ThrowsAndKeepsState()
    {
        var shipment = ShippedShipment();

        var exception = Assert.Throws<DomainException>(() => shipment.MarkFailed("  "));

        Assert.Equal("Failure reason is required.", exception.Message);
        Assert.Equal(ShipmentStatus.Shipped, shipment.Status);
        Assert.Null(shipment.FailureReason);
    }

    [Fact]
    public void MarkFailed_ReasonTooLong_Throws()
    {
        var shipment = ShippedShipment();

        var exception = Assert.Throws<DomainException>(() =>
            shipment.MarkFailed(new string('r', Shipment.FailureReasonMaxLength + 1)));

        Assert.Equal("Failure reason cannot exceed 500 characters.", exception.Message);
        Assert.Equal(ShipmentStatus.Shipped, shipment.Status);
        Assert.Null(shipment.FailureReason);
    }

    [Fact]
    public void MarkFailed_WhenAlreadyFailed_IsNoOpAndKeepsOriginalReason()
    {
        var shipment = FailedShipment();
        var updatedAt = shipment.UpdatedAtUtc;

        shipment.MarkFailed("Different late reason");

        Assert.Equal(ShipmentStatus.Failed, shipment.Status);
        Assert.Equal("Address unknown", shipment.FailureReason); // original kept
        Assert.Equal(updatedAt, shipment.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void MarkFailed_FromPending_ThrowsAndStaysPending()
    {
        var shipment = NewShipment();
        shipment.AddItem(Guid.NewGuid(), 1);

        var exception = Assert.Throws<DomainException>(() => shipment.MarkFailed("No carrier"));

        Assert.Equal("Shipment cannot move from Pending to Failed.", exception.Message);
        Assert.Equal(ShipmentStatus.Pending, shipment.Status);
        Assert.Null(shipment.FailureReason);
    }

    [Fact]
    public void MarkFailed_FromDelivered_ThrowsAndKeepsDelivery()
    {
        var shipment = DeliveredShipment();

        var exception = Assert.Throws<DomainException>(() => shipment.MarkFailed("Late notice"));

        Assert.Equal("Shipment cannot move from Delivered to Failed.", exception.Message);
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Null(shipment.FailureReason);
    }

    [Fact]
    public void MarkFailed_FromReturned_ThrowsAndKeepsReturn()
    {
        var shipment = ReturnedShipment();

        var exception = Assert.Throws<DomainException>(() => shipment.MarkFailed("Late notice"));

        Assert.Equal("Shipment cannot move from Returned to Failed.", exception.Message);
        Assert.Equal(ShipmentStatus.Returned, shipment.Status);
    }

    // ---------- ReturnToSender ----------

    [Fact]
    public void ReturnToSender_FromFailed_ReturnsPackage()
    {
        var shipment = FailedShipment();

        shipment.ReturnToSender();

        Assert.Equal(ShipmentStatus.Returned, shipment.Status);
        Assert.Equal("Address unknown", shipment.FailureReason); // audit trail kept
        Assert.True(shipment.UpdatedAtUtc >= shipment.CreatedAtUtc);
    }

    [Fact]
    public void ReturnToSender_WhenAlreadyReturned_IsNoOp()
    {
        var shipment = ReturnedShipment();
        var updatedAt = shipment.UpdatedAtUtc;

        shipment.ReturnToSender();

        Assert.Equal(ShipmentStatus.Returned, shipment.Status);
        Assert.Equal(updatedAt, shipment.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void ReturnToSender_FromInTransit_Throws()
    {
        var shipment = InTransitShipment();

        var exception = Assert.Throws<DomainException>(() => shipment.ReturnToSender());

        Assert.Equal("Shipment cannot move from InTransit to Returned.", exception.Message);
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
    }

    [Fact]
    public void ReturnToSender_FromDelivered_Throws()
    {
        var shipment = DeliveredShipment();

        var exception = Assert.Throws<DomainException>(() => shipment.ReturnToSender());

        Assert.Equal("Shipment cannot move from Delivered to Returned.", exception.Message);
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
    }

    // ---------- Cancel ----------

    [Fact]
    public void Cancel_FromPending_CancelsAndSetsTimestamp()
    {
        var shipment = NewShipment();
        shipment.AddItem(Guid.NewGuid(), 1);

        shipment.Cancel();

        Assert.Equal(ShipmentStatus.Cancelled, shipment.Status);
        Assert.NotNull(shipment.CancelledAtUtc);
        Assert.True(shipment.CancelledAtUtc >= shipment.CreatedAtUtc);
        Assert.Null(shipment.ShippedAtUtc);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_IsNoOp()
    {
        var shipment = CancelledShipment();
        var cancelledAt = shipment.CancelledAtUtc;
        var updatedAt = shipment.UpdatedAtUtc;

        shipment.Cancel();

        Assert.Equal(ShipmentStatus.Cancelled, shipment.Status);
        Assert.Equal(cancelledAt, shipment.CancelledAtUtc);
        Assert.Equal(updatedAt, shipment.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void Cancel_FromShipped_ThrowsAndStaysShipped()
    {
        var shipment = ShippedShipment();
        var shippedAt = shipment.ShippedAtUtc;

        var exception = Assert.Throws<DomainException>(() => shipment.Cancel());

        Assert.Equal("Shipment cannot move from Shipped to Cancelled.", exception.Message);
        Assert.Equal(ShipmentStatus.Shipped, shipment.Status);
        Assert.Equal(shippedAt, shipment.ShippedAtUtc);
        Assert.Null(shipment.CancelledAtUtc);
    }

    [Fact]
    public void Cancel_FromInTransit_Throws()
    {
        var shipment = InTransitShipment();

        var exception = Assert.Throws<DomainException>(() => shipment.Cancel());

        Assert.Equal("Shipment cannot move from InTransit to Cancelled.", exception.Message);
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
    }

    [Fact]
    public void Cancel_FromDelivered_Throws()
    {
        var shipment = DeliveredShipment();

        var exception = Assert.Throws<DomainException>(() => shipment.Cancel());

        Assert.Equal("Shipment cannot move from Delivered to Cancelled.", exception.Message);
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
    }

    // ---------- Terminal states ----------

    [Fact]
    public void ReturnedShipment_EveryFurtherTransition_Throws()
    {
        var shipment = ReturnedShipment();

        Assert.Throws<DomainException>(() => shipment.MarkShipped());
        Assert.Throws<DomainException>(() => shipment.MarkInTransit());
        Assert.Throws<DomainException>(() => shipment.MarkDelivered());
        Assert.Throws<DomainException>(() => shipment.MarkFailed("late notice"));
        Assert.Throws<DomainException>(() => shipment.Cancel());

        Assert.Equal(ShipmentStatus.Returned, shipment.Status);
        Assert.Equal("Address unknown", shipment.FailureReason); // untouched
    }

    [Fact]
    public void CancelledShipment_ItemsCannotBeAdded()
    {
        var shipment = CancelledShipment();

        var exception = Assert.Throws<DomainException>(() =>
            shipment.AddItem(Guid.NewGuid(), 1));

        Assert.Equal(
            "Shipment items can only be added while the shipment is pending.",
            exception.Message);
        Assert.Empty(shipment.Items);
    }
}
