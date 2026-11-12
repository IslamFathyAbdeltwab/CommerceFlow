using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Shipping.Queries;

// One DTO for detail and list reads: the items are nested because a shipment
// IS a box - the list view still shows what is inside (same call as AddressDto).
public sealed record ShipmentItemDto(
    Guid Id,
    Guid ProductVariantId,
    int Quantity);

public sealed record ShipmentDto(
    Guid Id,
    Guid OrderId,
    ShipmentStatus Status,
    string Carrier,
    string TrackingNumber,
    string? ServiceLevel,
    string? FailureReason,
    DateTimeOffset? ShippedAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    DateTimeOffset? CancelledAtUtc,
    IReadOnlyList<ShipmentItemDto> Items,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

// Centralizes entity -> DTO mapping so the handlers stay thin.
internal static class ShipmentMappings
{
    public static ShipmentDto ToDto(Shipment shipment) => new(
        shipment.Id,
        shipment.OrderId,
        shipment.Status,
        shipment.Carrier,
        shipment.TrackingNumber,
        shipment.ServiceLevel,
        shipment.FailureReason,
        shipment.ShippedAtUtc,
        shipment.DeliveredAtUtc,
        shipment.CancelledAtUtc,
        shipment.Items
            .Select(i => new ShipmentItemDto(i.Id, i.ProductVariantId, i.Quantity))
            .ToList(),
        shipment.CreatedAtUtc,
        shipment.UpdatedAtUtc);
}
