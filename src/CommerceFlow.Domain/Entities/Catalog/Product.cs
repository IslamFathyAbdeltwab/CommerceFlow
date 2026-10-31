using CommerceFlow.Domain.Catalog;
using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Catalog;

public class Product : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int SlugMaxLength = 200;

    private readonly List<ProductVariant> _variants = [];
    private readonly List<ProductImage> _images = [];
    private readonly List<ProductCategory> _categories = [];

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ProductStatus Status { get; private set; }
    public bool IsFeatured { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }

    // Brand is another aggregate, so Product references it by ID only.
    public Guid? BrandId { get; private set; }

    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();
    public IReadOnlyCollection<ProductCategory> Categories => _categories.AsReadOnly();

    private Product() { } // required by EF Core

    private Product(string name, string slug, string description, Guid? brandId)
    {
        Name = name;
        Slug = slug;
        Description = description;
        BrandId = brandId;
        Status = ProductStatus.Draft;
        IsFeatured = false;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    // ---------- Creation and details ----------

    public static Product Create(string name, string slug, string description, Guid? brandId)
    {
        var (n, s, d) = ValidateDetails(name, slug, description);
        return new Product(n, s, d, brandId);
    }

    public void UpdateDetails(string name, string slug, string description, Guid? brandId)
    {
        EnsureNotArchived();

        var (n, s, d) = ValidateDetails(name, slug, description);
        Name = n;
        Slug = s;
        Description = d;
        BrandId = brandId;
        Touch();
    }

    // ---------- Lifecycle ----------

    public void Publish()
    {
        if (Status != ProductStatus.Draft)
            throw new DomainException("Only a draft product can be published.");

        if (!_variants.Any(v => v.IsActive))
            throw new DomainException("A product needs at least one active variant to be published.");

        Status = ProductStatus.Published;
        PublishedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Archive()
    {
        if (Status == ProductStatus.Archived)
            throw new DomainException("Product is already archived.");

        Status = ProductStatus.Archived;
        IsFeatured = false;
        Touch();
    }

    public void Feature()
    {
        if (Status != ProductStatus.Published)
            throw new DomainException("Only a published product can be featured.");

        if (IsFeatured) return;

        IsFeatured = true;
        Touch();
    }

    public void Unfeature()
    {
        if (!IsFeatured) return;

        IsFeatured = false;
        Touch();
    }

    // ---------- Variants ----------

    public Guid AddVariant(string name, string sku, decimal price, decimal? compareAtPrice = null)
    {
        EnsureNotArchived();

        var normalizedSku = NormalizeSku(sku);
        if (_variants.Any(v => v.Sku == normalizedSku))
            throw new DomainException("Variant SKU already exists in this product.");

        var variant = new ProductVariant(Id, name, normalizedSku, price, compareAtPrice);
        _variants.Add(variant);
        Touch();
        return variant.Id;
    }

    public void ChangeVariantPrice(Guid variantId, decimal price, decimal? compareAtPrice = null)
    {
        EnsureNotArchived();

        GetVariant(variantId).ChangePrice(price, compareAtPrice);
        Touch();
    }

    public void ActivateVariant(Guid variantId)
    {
        EnsureNotArchived();

        GetVariant(variantId).Activate();
        Touch();
    }

    public void DeactivateVariant(Guid variantId)
    {
        EnsureNotArchived();

        var variant = GetVariant(variantId);
        EnsureStaysSellable(variant);

        variant.Deactivate();
        Touch();
    }

    public void RemoveVariant(Guid variantId)
    {
        EnsureNotArchived();

        var variant = GetVariant(variantId);
        EnsureStaysSellable(variant);

        _variants.Remove(variant);
        _images.RemoveAll(i => i.ProductVariantId == variantId); // images tagged to this variant go with it
        Touch();
    }

    // ---------- Images ----------

    public Guid AddImage(
        string storageKey,
        string? altText,
        int displayOrder,
        bool isPrimary = false,
        Guid? variantId = null)
    {
        EnsureNotArchived();

        if (variantId.HasValue && _variants.All(v => v.Id != variantId.Value))
            throw new DomainException("Image variant does not belong to this product.");

        if (isPrimary && variantId.HasValue)
            throw new DomainException("The primary image must be a product-level image, not a variant image.");

        // The first product-level image becomes primary automatically.
        var makePrimary = isPrimary || (variantId is null && !_images.Any(i => i.IsPrimary));

        if (makePrimary)
            foreach (var existing in _images)
                existing.UnmarkPrimary();

        var image = new ProductImage(Id, variantId, storageKey, altText, displayOrder, makePrimary);
        _images.Add(image);
        Touch();
        return image.Id;
    }

    public void SetPrimaryImage(Guid imageId)
    {
        EnsureNotArchived();

        var image = GetImage(imageId);
        if (image.ProductVariantId.HasValue)
            throw new DomainException("The primary image must be a product-level image, not a variant image.");

        foreach (var existing in _images)
            existing.UnmarkPrimary();

        image.MarkAsPrimary();
        Touch();
    }

    public void RemoveImage(Guid imageId)
    {
        EnsureNotArchived();

        var image = GetImage(imageId);
        _images.Remove(image);

        if (image.IsPrimary)
            _images
                .Where(i => i.ProductVariantId is null)
                .OrderBy(i => i.DisplayOrder)
                .FirstOrDefault()
                ?.MarkAsPrimary();

        Touch();
    }

    // ---------- Categories ----------

    public void AddCategory(Guid categoryId)
    {
        EnsureNotArchived();

        if (categoryId == Guid.Empty)
            throw new DomainException("Category ID is required.");

        if (_categories.Any(c => c.CategoryId == categoryId))
            throw new DomainException("Product already belongs to this category.");

        _categories.Add(new ProductCategory(Id, categoryId));
        Touch();
    }

    public void RemoveCategory(Guid categoryId)
    {
        EnsureNotArchived();

        var category = _categories.FirstOrDefault(c => c.CategoryId == categoryId)
            ?? throw new DomainException("Product category not found.");

        _categories.Remove(category);
        Touch();
    }

    // ---------- Private helpers ----------

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private void EnsureNotArchived()
    {
        if (Status == ProductStatus.Archived)
            throw new DomainException("An archived product cannot be modified.");
    }

    // A published product must always keep at least one active variant.
    private void EnsureStaysSellable(ProductVariant variant)
    {
        if (Status == ProductStatus.Published
            && variant.IsActive
            && !_variants.Any(v => v.Id != variant.Id && v.IsActive))
        {
            throw new DomainException("A published product must keep at least one active variant.");
        }
    }

    private ProductVariant GetVariant(Guid variantId) =>
        _variants.FirstOrDefault(v => v.Id == variantId)
        ?? throw new DomainException("Variant not found.");

    private ProductImage GetImage(Guid imageId) =>
        _images.FirstOrDefault(i => i.Id == imageId)
        ?? throw new DomainException("Image not found.");

    private static string NormalizeSku(string? sku) =>
        (sku ?? string.Empty).Trim().ToUpperInvariant();

    private static (string Name, string Slug, string Description) ValidateDetails(
        string? name, string? slug, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required.");
        name = name.Trim();
        if (name.Length > NameMaxLength)
            throw new DomainException($"Product name cannot exceed {NameMaxLength} characters.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Product slug is required.");
        slug = slug.Trim().ToLowerInvariant();
        if (slug.Length > SlugMaxLength)
            throw new DomainException($"Product slug cannot exceed {SlugMaxLength} characters.");
        if (slug.Any(char.IsWhiteSpace))
            throw new DomainException("Product slug cannot contain spaces.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Product description is required.");

        return (name, slug, description.Trim());
    }
}