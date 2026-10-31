using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Catalog;

public class ProductCategory
{
    public Guid ProductId { get; private set; }
    public Guid CategoryId { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; private set; }

    private ProductCategory() { } // required by EF Core

    // internal: only the Product aggregate root can create this link.
    internal ProductCategory(Guid productId, Guid categoryId)
    {
        if (productId == Guid.Empty)
            throw new DomainException("Product ID is required.");

        if (categoryId == Guid.Empty)
            throw new DomainException("Category ID is required.");

        ProductId = productId;
        CategoryId = categoryId;
        AssignedAtUtc = DateTimeOffset.UtcNow;
    }
}