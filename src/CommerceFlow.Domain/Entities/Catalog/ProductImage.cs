using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Common.Entitiy;

namespace CommerceFlow.Domain.Catalog;

public class ProductImage : Entity
{
    public Guid ProductId { get; private set; }

    public string ImageUrl { get; private set; }

    public string? AltText { get; private set; }

    public bool IsPrimary { get; private set; }

    public int DisplayOrder { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ProductImage(
        Guid productId,
        string imageUrl,
        string? altText,
        bool isPrimary,
        int displayOrder)
    {
        ProductId = productId;
        ImageUrl = imageUrl;
        AltText = altText;
        IsPrimary = isPrimary;
        DisplayOrder = displayOrder;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static ProductImage Create(
        Guid productId,
        string imageUrl,
        string? altText,
        bool isPrimary,
        int displayOrder)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.");

        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("Image URL is required.");

        if (displayOrder < 0)
            throw new ArgumentException("Display order cannot be negative.");

        return new ProductImage(
            productId,
            imageUrl,
            altText,
            isPrimary,
            displayOrder);
    }

    public void UpdateDetails(string imageUrl, string? altText, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("Image URL is required.");

        if (displayOrder < 0)
            throw new ArgumentException("Display order cannot be negative.");

        ImageUrl = imageUrl;
        AltText = altText;
        DisplayOrder = displayOrder;
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void RemoveAsPrimary()
    {
        IsPrimary = false;
    }
}