using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Common.Entitiy;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Catalog;

public class ProductVariant : AuditableEntity
{
    public const int NameMaxLength = 200;
    public const int SkuMaxLength = 64;
    public const int BarcodeMaxLength = 64;

    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string? Barcode { get; private set; }
    public decimal Price { get; private set; }
    public decimal? CompareAtPrice { get; private set; }
    public int? WeightGrams { get; private set; }
    public bool IsActive { get; private set; }

    private ProductVariant() { } //  required by EF Core must have paramter lesss constractuor construcator 

    // internal: only the Product aggregate root can create a variant.
    // this is the ddd the parent or root responsable for create children not others
    internal ProductVariant(
        Guid productId,
        string name,
        string sku,
        decimal price,
        decimal? compareAtPrice)
    {
        if (productId == Guid.Empty)
            throw new DomainException("Product ID is required.");

        var (validName, validSku) = ValidateIdentity(name, sku);
        ValidatePricing(price, compareAtPrice);

        ProductId = productId;
        Name = validName;
        Sku = validSku;
        Price = price;
        CompareAtPrice = compareAtPrice;
        IsActive = true;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    internal void UpdateDetails(
        string name,
        string sku,
        string? barcode,
        decimal price,
        decimal? compareAtPrice,
        int? weightGrams)
    {
        var (validName, validSku) = ValidateIdentity(name, sku);
        ValidatePricing(price, compareAtPrice);

        if (weightGrams < 0)
            throw new DomainException("Weight cannot be negative.");

        var validBarcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        if (validBarcode is { Length: > BarcodeMaxLength })
            throw new DomainException($"Barcode cannot exceed {BarcodeMaxLength} characters.");

        Name = validName;
        Sku = validSku;
        Barcode = validBarcode;
        Price = price;
        CompareAtPrice = compareAtPrice;
        WeightGrams = weightGrams;
        Touch();
    }

    internal void ChangePrice(decimal price, decimal? compareAtPrice)
    {
        ValidatePricing(price, compareAtPrice);

        Price = price;
        CompareAtPrice = compareAtPrice;
        Touch();
    }

    internal void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        Touch();
    }

    internal void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static (string Name, string Sku) ValidateIdentity(string? name, string? sku)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Variant name is required.");
        name = name.Trim();
        if (name.Length > NameMaxLength)
            throw new DomainException($"Variant name cannot exceed {NameMaxLength} characters.");

        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU is required.");
        sku = sku.Trim().ToUpperInvariant();
        if (sku.Length > SkuMaxLength)
            throw new DomainException($"SKU cannot exceed {SkuMaxLength} characters.");

        return (name, sku);
    }

    private static void ValidatePricing(decimal price, decimal? compareAtPrice)
    {
        if (price < 0)
            throw new DomainException("Price cannot be negative.");

        if (compareAtPrice.HasValue && compareAtPrice.Value <= price)
            throw new DomainException("Compare-at price must be greater than the price.");
    }
    private readonly List<ProductVariantAttributeValue> _attributeValues = [];
    public IReadOnlyCollection<ProductVariantAttributeValue> AttributeValues => _attributeValues.AsReadOnly();

    internal void SetAttributeValue(Guid attributeId, string value)
    {
        var existing = _attributeValues.FirstOrDefault(a => a.ProductAttributeId == attributeId);

        if (existing is null)
            _attributeValues.Add(new ProductVariantAttributeValue(Id, attributeId, value));
        else
            existing.ChangeValue(value);

        Touch();
    }

    internal void RemoveAttributeValue(Guid attributeId)
    {
        var existing = _attributeValues.FirstOrDefault(a => a.ProductAttributeId == attributeId)
            ?? throw new DomainException("Attribute value not found.");

        _attributeValues.Remove(existing);
        Touch();
    }
}