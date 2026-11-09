using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Inventory;

public class InventoryReservationTests
{
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();
    private static readonly Guid VariantId = Guid.NewGuid();

    private static InventoryReservation NewReservation() =>
        InventoryReservation.Create(OrderId, DateTimeOffset.UtcNow.AddHours(1));

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_StartsActiveAndEmpty()
    {
        var reservation = NewReservation();

        Assert.Equal(OrderId, reservation.OrderId);
        Assert.Equal(InventoryReservationStatus.Active, reservation.Status);
        Assert.Empty(reservation.Items);
    }

    [Fact]
    public void Create_EmptyOrderId_Throws()
    {
        Assert.Throws<DomainException>(() => InventoryReservation.Create(Guid.Empty));
    }

    [Fact]
    public void Create_ExpiresAtInPast_Throws()
    {
        Assert.Throws<DomainException>(() =>
            InventoryReservation.Create(OrderId, DateTimeOffset.UtcNow.AddMinutes(-1)));
    }

    // ---------- AddItem ----------

    [Fact]
    public void AddItem_ValidData_AddsItemWithReservationId()
    {
        var reservation = NewReservation();

        reservation.AddItem(WarehouseId, VariantId, 2);

        var item = Assert.Single(reservation.Items);
        Assert.Equal(reservation.Id, item.InventoryReservationId);
        Assert.Equal(WarehouseId, item.WarehouseId);
        Assert.Equal(VariantId, item.ProductVariantId);
        Assert.Equal(2, item.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_NonPositiveQuantity_Throws(int qty)
    {
        var reservation = NewReservation();
        Assert.Throws<DomainException>(() => reservation.AddItem(WarehouseId, VariantId, qty));
    }

    [Fact]
    public void AddItem_DuplicateWarehouseAndVariant_Throws()
    {
        var reservation = NewReservation();
        reservation.AddItem(WarehouseId, VariantId, 1);

        Assert.Throws<DomainException>(() => reservation.AddItem(WarehouseId, VariantId, 1));
    }

    [Fact]
    public void AddItem_AfterRelease_Throws()
    {
        var reservation = NewReservation();
        reservation.AddItem(WarehouseId, VariantId, 1);
        reservation.Release();

        Assert.Throws<DomainException>(() => reservation.AddItem(Guid.NewGuid(), VariantId, 1));
    }

    // ---------- State transitions ----------

    [Fact]
    public void Consume_Active_BecomesConsumed()
    {
        var reservation = NewReservation();
        reservation.Consume();
        Assert.Equal(InventoryReservationStatus.Consumed, reservation.Status);
    }

    [Fact]
    public void Release_SetsReleasedAtUtc()
    {
        var reservation = NewReservation();
        reservation.Release();

        Assert.Equal(InventoryReservationStatus.Released, reservation.Status);
        Assert.NotNull(reservation.ReleasedAtUtc);
    }

    [Fact]
    public void Expire_BecomesExpired()
    {
        var reservation = NewReservation();
        reservation.Expire();
        Assert.Equal(InventoryReservationStatus.Expired, reservation.Status);
    }

    [Fact]
    public void Consume_AfterRelease_Throws()
    {
        var reservation = NewReservation();
        reservation.Release();

        Assert.Throws<DomainException>(() => reservation.Consume());
    }

    [Fact]
    public void Release_AfterConsume_Throws()
    {
        var reservation = NewReservation();
        reservation.Consume();

        Assert.Throws<DomainException>(() => reservation.Release());
    }

    [Fact]
    public void Expire_AfterConsume_Throws()
    {
        var reservation = NewReservation();
        reservation.Consume();

        Assert.Throws<DomainException>(() => reservation.Expire());
    }
}
