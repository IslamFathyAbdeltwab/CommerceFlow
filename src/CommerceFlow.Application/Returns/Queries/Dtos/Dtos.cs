using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Returns.Queries;

// One DTO for detail and list reads: the items are nested because a return
// request IS the basket of goods (same call as ShipmentDto).
public sealed record ReturnItemDto(
    Guid Id,
    Guid ProductVariantId,
    int Quantity);

public sealed record ReturnRequestDto(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    ReturnStatus Status,
    ReturnReason Reason,
    string? Notes,
    string? RejectionReason,
    DateTimeOffset? ApprovedAtUtc,
    DateTimeOffset? RejectedAtUtc,
    DateTimeOffset? ReceivedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    IReadOnlyList<ReturnItemDto> Items,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

// Centralizes entity -> DTO mapping so the handlers stay thin.
internal static class ReturnRequestMappings
{
    public static ReturnRequestDto ToDto(ReturnRequest request) => new(
        request.Id,
        request.OrderId,
        request.CustomerId,
        request.Status,
        request.Reason,
        request.Notes,
        request.RejectionReason,
        request.ApprovedAtUtc,
        request.RejectedAtUtc,
        request.ReceivedAtUtc,
        request.CompletedAtUtc,
        request.CancelledAtUtc,
        request.Items
            .Select(i => new ReturnItemDto(i.Id, i.ProductVariantId, i.Quantity))
            .ToList(),
        request.CreatedAtUtc,
        request.UpdatedAtUtc);
}
