using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Promotions;

// One redemption of a promotion against an order - the audit row that makes
// the usage limits (total and per-customer) countable. Order is referenced by
// ID only (another aggregate, like ShipmentItem's order reference); the money
// itself was taken on the order by Cart/Order through the Application layer,
// DiscountTaken is just what this campaign contributed.
public class PromotionUsage
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid PromotionId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid OrderId { get; private set; }

    // What the campaign actually took off that order (may be less than the
    // nominal value - a fixed discount never exceeds the order amount).
    public decimal DiscountTaken { get; private set; }

    public DateTimeOffset UsedAtUtc { get; private set; }

    private PromotionUsage() { } // required by EF Core

    // internal: only the Promotion aggregate root records usages.
    internal PromotionUsage(
        Guid promotionId,
        Guid customerId,
        Guid orderId,
        decimal discountTaken)
    {
        if (promotionId == Guid.Empty)
            throw new DomainException("Promotion ID is required.");

        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required.");

        if (orderId == Guid.Empty)
            throw new DomainException("Order ID is required.");

        if (discountTaken <= 0)
            throw new DomainException("Discount taken must be positive.");

        PromotionId = promotionId;
        CustomerId = customerId;
        OrderId = orderId;
        DiscountTaken = discountTaken;
        UsedAtUtc = DateTimeOffset.UtcNow;
    }
}
