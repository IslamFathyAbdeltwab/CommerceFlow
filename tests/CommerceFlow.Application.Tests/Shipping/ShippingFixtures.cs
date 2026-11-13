using CommerceFlow.Application.Shipping.Commands.CreateShipment;
using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Tests.Shipping;

// Shared factories for building shipments (and the create command) in tests.
// The fixture puts at least one item in the box by default because MarkShipped
// refuses an empty box; pass withItem: false only to exercise that guard.
internal static class ShippingFixtures
{
    public static readonly Guid OrderId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    public static readonly Guid VariantId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public static Shipment CreateShipment(
        Guid? orderId = null,
        ShipmentStatus status = ShipmentStatus.Pending,
        Guid? productVariantId = null,
        int quantity = 2,
        string carrier = "DHL",
        string trackingNumber = "TRACK-001",
        string? serviceLevel = "Express",
        string? failureReason = null,
        bool withItem = true)
    {
        var shipment = Shipment.Create(
            orderId ?? OrderId,
            carrier,
            trackingNumber,
            serviceLevel);

        if (withItem)
            shipment.AddItem(productVariantId ?? VariantId, quantity);

        switch (status)
        {
            case ShipmentStatus.Pending:
                break;
            case ShipmentStatus.Shipped:
                shipment.MarkShipped();
                break;
            case ShipmentStatus.InTransit:
                shipment.MarkShipped();
                shipment.MarkInTransit();
                break;
            case ShipmentStatus.Delivered:
                shipment.MarkShipped();
                shipment.MarkDelivered();
                break;
            case ShipmentStatus.Failed:
                shipment.MarkShipped();
                shipment.MarkFailed(failureReason ?? "Nobody home.");
                break;
            case ShipmentStatus.Returned:
                shipment.MarkShipped();
                shipment.MarkFailed(failureReason ?? "Nobody home.");
                shipment.ReturnToSender();
                break;
            case ShipmentStatus.Cancelled:
                shipment.Cancel();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        return shipment;
    }

    public static CreateShipmentCommand ValidCreateCommand(
        Guid? orderId = null,
        Guid? productVariantId = null,
        int quantity = 1,
        string carrier = "DHL",
        string trackingNumber = "TRACK-001",
        string? serviceLevel = "Express") =>
        new(
            orderId ?? OrderId,
            carrier,
            trackingNumber,
            [new CreateShipmentItemDto(productVariantId ?? VariantId, quantity)],
            serviceLevel);
}
