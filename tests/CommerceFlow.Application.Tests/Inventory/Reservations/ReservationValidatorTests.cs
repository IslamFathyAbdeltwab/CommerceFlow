using CommerceFlow.Application.Inventory.Reservations.Commands.CreateInventoryReservation;

namespace CommerceFlow.Application.Tests.Inventory.Reservations;

public sealed class ReservationValidatorTests
{
    private static CreateInventoryReservationCommand Command(
        Guid orderId,
        IReadOnlyList<CreateInventoryReservationItemDto> items,
        DateTimeOffset? expiresAtUtc = null) =>
        new(orderId, items, expiresAtUtc);

    private static CreateInventoryReservationItemDto Item(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        int quantity = 1) =>
        new(warehouseId ?? Guid.NewGuid(), productVariantId ?? Guid.NewGuid(), quantity);

    [Fact]
    public void CreateReservation_WithValidInput_IsValid()
    {
        var result = new CreateInventoryReservationCommandValidator()
            .Validate(Command(Guid.NewGuid(), new List<CreateInventoryReservationItemDto> { Item() }));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateReservation_WithEmptyOrderId_IsInvalid()
    {
        var result = new CreateInventoryReservationCommandValidator()
            .Validate(Command(Guid.Empty, new List<CreateInventoryReservationItemDto> { Item() }));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateInventoryReservationCommand.OrderId));
    }

    [Fact]
    public void CreateReservation_WithEmptyItems_IsInvalid()
    {
        var result = new CreateInventoryReservationCommandValidator()
            .Validate(Command(Guid.NewGuid(), new List<CreateInventoryReservationItemDto>()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateInventoryReservationCommand.Items));
    }

    [Fact]
    public void CreateReservation_WithEmptyWarehouseIdOnItem_IsInvalid()
    {
        var result = new CreateInventoryReservationCommandValidator()
            .Validate(Command(
                Guid.NewGuid(),
                new List<CreateInventoryReservationItemDto> { Item(warehouseId: Guid.Empty) }));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Items[0].WarehouseId");
    }

    [Fact]
    public void CreateReservation_WithNonPositiveQuantityOnItem_IsInvalid()
    {
        var result = new CreateInventoryReservationCommandValidator()
            .Validate(Command(
                Guid.NewGuid(),
                new List<CreateInventoryReservationItemDto> { Item(quantity: 0) }));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Items[0].Quantity");
    }

    [Fact]
    public void CreateReservation_WithPastExpiry_IsInvalid()
    {
        var result = new CreateInventoryReservationCommandValidator()
            .Validate(Command(
                Guid.NewGuid(),
                new List<CreateInventoryReservationItemDto> { Item() },
                DateTimeOffset.UtcNow.AddMinutes(-5)));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage == "Expires-at must be in the future.");
    }

    [Fact]
    public void CreateReservation_WithFutureExpiry_IsValid()
    {
        var result = new CreateInventoryReservationCommandValidator()
            .Validate(Command(
                Guid.NewGuid(),
                new List<CreateInventoryReservationItemDto> { Item() },
                DateTimeOffset.UtcNow.AddHours(1)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateReservation_WithNullExpiry_IsValid()
    {
        var result = new CreateInventoryReservationCommandValidator()
            .Validate(Command(Guid.NewGuid(), new List<CreateInventoryReservationItemDto> { Item() }, null));

        Assert.True(result.IsValid);
    }
}
