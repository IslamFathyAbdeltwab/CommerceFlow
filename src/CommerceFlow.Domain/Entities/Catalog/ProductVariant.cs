using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Common.Entitiy;

namespace CommerceFlow.Domain.Catalog;

public class ProductVariant : AuditableEntity
{
    public Guid ProductId { get; private set; }

    public string Name { get; private set; }
    public string Sku { get; private set; }
    public string? Barcode { get; private set; }

    public decimal Price { get; private set; }
    public decimal? CompareAtPrice { get; private set; }

    public int? WeightGrams { get; private set; }

    public bool IsActive { get; private set; }

    private ProductVariant(
        string name,
        string sku,
        decimal price)
    {
        Name = name;
        Sku = sku;
        Price = price;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static ProductVariant Create(
        string name,
        string sku,
        decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variant name is required.");

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.");

        if (price < 0)
            throw new ArgumentException("Price cannot be negative.");

        return new ProductVariant(name, sku, price);
    }

    public void UpdateDetails(
        string name,
        string sku,
        string? barcode,
        decimal price,
        decimal? compareAtPrice,
        int? weightGrams)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variant name is required.");

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.");

        if (price < 0)
            throw new ArgumentException("Price cannot be negative.");

        if (compareAtPrice < 0)
            throw new ArgumentException("Compare-at price cannot be negative.");

        if (weightGrams < 0)
            throw new ArgumentException("Weight cannot be negative.");

        Name = name;
        Sku = sku;
        Barcode = barcode;
        Price = price;
        CompareAtPrice = compareAtPrice;
        WeightGrams = weightGrams;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}