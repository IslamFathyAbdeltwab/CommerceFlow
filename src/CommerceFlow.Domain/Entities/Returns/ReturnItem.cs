using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Returns;

// One variant going back to us. Variant is a Catalog aggregate - reference by
// ID only, like ShipmentItem and OrderLine do.
public class ReturnItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ReturnRequestId { get; private set; }

    // Variant is a Catalog aggregate - reference by ID only.
    public Guid ProductVariantId { get; private set; }

    public int Quantity { get; private set; }

    private ReturnItem() { } // required by EF Core

    // internal: only the ReturnRequest aggregate root creates items.
    internal ReturnItem(
        Guid returnRequestId,
        Guid productVariantId,
        int quantity)
    {
        if (returnRequestId == Guid.Empty)
            throw new DomainException("Return request ID is required.");

        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant ID is required.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        ReturnRequestId = returnRequestId;
        ProductVariantId = productVariantId;
        Quantity = quantity;
    }
}
