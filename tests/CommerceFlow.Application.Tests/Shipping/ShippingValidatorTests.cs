using CommerceFlow.Application.Shipping.Commands.CancelShipment;
using CommerceFlow.Application.Shipping.Commands.CreateShipment;
using CommerceFlow.Application.Shipping.Commands.DeliverShipment;
using CommerceFlow.Application.Shipping.Commands.FailShipment;
using CommerceFlow.Application.Shipping.Commands.MarkShipmentInTransit;
using CommerceFlow.Application.Shipping.Commands.ReturnShipment;
using CommerceFlow.Application.Shipping.Commands.ShipShipment;
using CommerceFlow.Application.Shipping.Queries.GetShipments;
using CommerceFlow.Domain.Entities.Shipping;

namespace CommerceFlow.Application.Tests.Shipping;

public sealed class ShippingValidatorTests
{
    private static string TooLong(int length) => new('x', length);

    // ---------- CreateShipment ----------

    [Fact]
    public void Create_WithValidInput_IsValid()
    {
        var result = new CreateShipmentCommandValidator()
            .Validate(ShippingFixtures.ValidCreateCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_WithEmptyOrderId_IsInvalid()
    {
        var result = new CreateShipmentCommandValidator()
            .Validate(ShippingFixtures.ValidCreateCommand() with { OrderId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateShipmentCommand.OrderId));
    }

    [Fact]
    public void Create_WithEmptyCarrier_IsInvalid()
    {
        var result = new CreateShipmentCommandValidator()
            .Validate(ShippingFixtures.ValidCreateCommand() with { Carrier = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateShipmentCommand.Carrier));
    }

    [Fact]
    public void Create_WithTooLongCarrier_IsInvalid()
    {
        var result = new CreateShipmentCommandValidator()
            .Validate(ShippingFixtures.ValidCreateCommand() with
            {
                Carrier = TooLong(Shipment.CarrierMaxLength + 1)
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateShipmentCommand.Carrier));
    }

    [Fact]
    public void Create_WithEmptyTrackingNumber_IsInvalid()
    {
        var result = new CreateShipmentCommandValidator()
            .Validate(ShippingFixtures.ValidCreateCommand() with { TrackingNumber = " " });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateShipmentCommand.TrackingNumber));
    }

    [Fact]
    public void Create_WithTooLongTrackingNumber_IsInvalid()
    {
        var result = new CreateShipmentCommandValidator()
            .Validate(ShippingFixtures.ValidCreateCommand() with
            {
                TrackingNumber = TooLong(Shipment.TrackingNumberMaxLength + 1)
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateShipmentCommand.TrackingNumber));
    }

    [Fact]
    public void Create_WithTooLongServiceLevel_IsInvalid()
    {
        var result = new CreateShipmentCommandValidator()
            .Validate(ShippingFixtures.ValidCreateCommand() with
            {
                ServiceLevel = TooLong(Shipment.ServiceLevelMaxLength + 1)
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateShipmentCommand.ServiceLevel));
    }

    [Fact]
    public void Create_WithNullServiceLevel_IsValid()
    {
        var result = new CreateShipmentCommandValidator()
            .Validate(ShippingFixtures.ValidCreateCommand() with { ServiceLevel = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_WithNoItems_IsInvalid()
    {
        var result = new CreateShipmentCommandValidator()
            .Validate(ShippingFixtures.ValidCreateCommand() with { Items = [] });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateShipmentCommand.Items));
    }

    [Fact]
    public void Create_WithEmptyItemVariantId_IsInvalid()
    {
        var result = new CreateShipmentCommandValidator()
            .Validate(ShippingFixtures.ValidCreateCommand(productVariantId: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName!.Contains(nameof(CreateShipmentItemDto.ProductVariantId)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveItemQuantity_IsInvalid(int quantity)
    {
        var result = new CreateShipmentCommandValidator()
            .Validate(ShippingFixtures.ValidCreateCommand(quantity: quantity));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName!.Contains(nameof(CreateShipmentItemDto.Quantity)));
    }

    // ---------- ShipShipment / MarkShipmentInTransit / DeliverShipment ----------

    [Fact]
    public void Ship_WithValidInput_IsValid()
    {
        var result = new ShipShipmentCommandValidator()
            .Validate(new ShipShipmentCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Ship_WithEmptyShipmentId_IsInvalid()
    {
        var result = new ShipShipmentCommandValidator()
            .Validate(new ShipShipmentCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(ShipShipmentCommand.ShipmentId));
    }

    [Fact]
    public void InTransit_WithValidInput_IsValid()
    {
        var result = new MarkShipmentInTransitCommandValidator()
            .Validate(new MarkShipmentInTransitCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void InTransit_WithEmptyShipmentId_IsInvalid()
    {
        var result = new MarkShipmentInTransitCommandValidator()
            .Validate(new MarkShipmentInTransitCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(MarkShipmentInTransitCommand.ShipmentId));
    }

    [Fact]
    public void Deliver_WithValidInput_IsValid()
    {
        var result = new DeliverShipmentCommandValidator()
            .Validate(new DeliverShipmentCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Deliver_WithEmptyShipmentId_IsInvalid()
    {
        var result = new DeliverShipmentCommandValidator()
            .Validate(new DeliverShipmentCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(DeliverShipmentCommand.ShipmentId));
    }

    // ---------- FailShipment ----------

    [Fact]
    public void Fail_WithValidInput_IsValid()
    {
        var result = new FailShipmentCommandValidator()
            .Validate(new FailShipmentCommand(Guid.NewGuid(), "Nobody home."));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Fail_WithEmptyShipmentId_IsInvalid()
    {
        var result = new FailShipmentCommandValidator()
            .Validate(new FailShipmentCommand(Guid.Empty, "Nobody home."));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(FailShipmentCommand.ShipmentId));
    }

    [Fact]
    public void Fail_WithEmptyReason_IsInvalid()
    {
        var result = new FailShipmentCommandValidator()
            .Validate(new FailShipmentCommand(Guid.NewGuid(), ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(FailShipmentCommand.Reason));
    }

    [Fact]
    public void Fail_WithTooLongReason_IsInvalid()
    {
        var result = new FailShipmentCommandValidator()
            .Validate(new FailShipmentCommand(
                Guid.NewGuid(), TooLong(Shipment.FailureReasonMaxLength + 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(FailShipmentCommand.Reason));
    }

    // ---------- ReturnShipment / CancelShipment ----------

    [Fact]
    public void Return_WithValidInput_IsValid()
    {
        var result = new ReturnShipmentCommandValidator()
            .Validate(new ReturnShipmentCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Return_WithEmptyShipmentId_IsInvalid()
    {
        var result = new ReturnShipmentCommandValidator()
            .Validate(new ReturnShipmentCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(ReturnShipmentCommand.ShipmentId));
    }

    [Fact]
    public void Cancel_WithValidInput_IsValid()
    {
        var result = new CancelShipmentCommandValidator()
            .Validate(new CancelShipmentCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Cancel_WithEmptyShipmentId_IsInvalid()
    {
        var result = new CancelShipmentCommandValidator()
            .Validate(new CancelShipmentCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CancelShipmentCommand.ShipmentId));
    }

    // ---------- GetShipments (query) ----------

    [Fact]
    public void GetShipments_WithDefaults_IsValid()
    {
        var result = new GetShipmentsQueryValidator().Validate(new GetShipmentsQuery());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void GetShipments_WithOutOfRangePageNumber_IsInvalid(int pageNumber)
    {
        var result = new GetShipmentsQueryValidator()
            .Validate(new GetShipmentsQuery(PageNumber: pageNumber));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(GetShipmentsQuery.PageNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void GetShipments_WithOutOfRangePageSize_IsInvalid(int pageSize)
    {
        var result = new GetShipmentsQueryValidator()
            .Validate(new GetShipmentsQuery(PageSize: pageSize));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(GetShipmentsQuery.PageSize));
    }
}
