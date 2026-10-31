using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Common.Entitiy;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Catalog;

public class ProductImage : Entity
{
    public const int StorageKeyMaxLength = 500;
    public const int AltTextMaxLength = 250;

    public Guid ProductId { get; private set; }

    // Optional: null = general product image, set = image for one specific variant.
    public Guid? ProductVariantId { get; private set; }

    // Key/path of the file in object storage, not the file itself.
    public string StorageKey { get; private set; } = string.Empty;
    public string? AltText { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ProductImage() { } // required by EF Core

    // internal: only the Product aggregate root can create an image.
    internal ProductImage(
        Guid productId,
        Guid? productVariantId,
        string storageKey,
        string? altText,
        int displayOrder,
        bool isPrimary)
    {
        if (productId == Guid.Empty)
            throw new DomainException("Product ID is required.");

        if (productVariantId == Guid.Empty)
            throw new DomainException("Variant ID cannot be empty.");

        if (isPrimary && productVariantId.HasValue)
            throw new DomainException("The primary image must be a product-level image, not a variant image.");

        var (key, alt) = Validate(storageKey, altText, displayOrder);

        ProductId = productId;
        ProductVariantId = productVariantId;
        StorageKey = key;
        AltText = alt;
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    internal void UpdateDetails(string storageKey, string? altText, int displayOrder)
    {
        var (key, alt) = Validate(storageKey, altText, displayOrder);

        StorageKey = key;
        AltText = alt;
        DisplayOrder = displayOrder;
    }

    internal void MarkAsPrimary()
    {
        if (ProductVariantId.HasValue)
            throw new DomainException("The primary image must be a product-level image, not a variant image.");

        IsPrimary = true;
    }

    internal void UnmarkPrimary() => IsPrimary = false;

    private static (string StorageKey, string? AltText) Validate(
        string? storageKey, string? altText, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new DomainException("Image storage key is required.");
        storageKey = storageKey.Trim();
        if (storageKey.Length > StorageKeyMaxLength)
            throw new DomainException($"Image storage key cannot exceed {StorageKeyMaxLength} characters.");

        if (displayOrder < 0)
            throw new DomainException("Display order cannot be negative.");

        var alt = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        if (alt is { Length: > AltTextMaxLength })
            throw new DomainException($"Alt text cannot exceed {AltTextMaxLength} characters.");

        return (storageKey, alt);
    }
}