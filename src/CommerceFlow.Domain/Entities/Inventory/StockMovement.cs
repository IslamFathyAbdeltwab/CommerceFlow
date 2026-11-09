using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Inventory;

// Append-only history of physical on-hand changes.
// Never edit/delete to fix an error; add a ManualAdjustment instead.
// Aggregate root by Id (Guid) even though it is immutable after Create.
public class StockMovement : AggregateRoot
{
    public Guid WarehouseId { get; private set; }

    // Variant is a Catalog aggregate - reference by ID only.
    public Guid ProductVariantId { get; private set; }

    public StockMovementType MovementType { get; private set; }

    public int QuantityChange { get; private set; }
    public int? QuantityAfter { get; private set; }

    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }

    public string Reason { get; private set; } = string.Empty;
    public Guid? PerformedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtcMovement { get; private set; }

    private StockMovement() { } // required by EF Core

    private StockMovement(
        Guid warehouseId,
        Guid productVariantId,
        StockMovementType movementType,
        int quantityChange,
        int? quantityAfter,
        string? referenceType,
        Guid? referenceId,
        string reason,
        Guid? performedByUserId,
        DateTimeOffset createdAtUtc)
    {
        WarehouseId = warehouseId;
        ProductVariantId = productVariantId;
        MovementType = movementType;
        QuantityChange = quantityChange;
        QuantityAfter = quantityAfter;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Reason = reason;
        PerformedByUserId = performedByUserId;
        CreatedAtUtcMovement = createdAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public static StockMovement Create(
        Guid warehouseId,
        Guid productVariantId,
        StockMovementType movementType,
        int quantityChange,
        int? quantityAfter,
        string reason,
        string? referenceType = null,
        Guid? referenceId = null,
        Guid? performedByUserId = null)
    {
        if (warehouseId == Guid.Empty)
            throw new DomainException("Warehouse ID is required.");

        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant ID is required.");

        if (!Enum.IsDefined(movementType))
            throw new DomainException("Invalid movement type.");

        if (quantityChange == 0)
            throw new DomainException("Quantity change cannot be zero.");

        if (quantityAfter.HasValue && quantityAfter.Value < 0)
            throw new DomainException("Quantity after cannot be negative.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Reason is required.");

        var trimmedReason = reason.Trim();
        if (trimmedReason.Length > 500)
            throw new DomainException("Reason cannot exceed 500 characters.");

        var trimmedRefType = string.IsNullOrWhiteSpace(referenceType) ? null : referenceType.Trim();
        if (trimmedRefType is { Length: > 100 })
            throw new DomainException("Reference type cannot exceed 100 characters.");

        return new StockMovement(
            warehouseId,
            productVariantId,
            movementType,
            quantityChange,
            quantityAfter,
            trimmedRefType,
            referenceId,
            trimmedReason,
            performedByUserId,
            DateTimeOffset.UtcNow);
    }
}
