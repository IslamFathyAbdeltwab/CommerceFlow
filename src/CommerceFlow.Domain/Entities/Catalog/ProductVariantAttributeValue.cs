using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Common.Entitiy;

namespace CommerceFlow.Domain.Catalog;

public class ProductVariantAttributeValue : Entity
{
    public Guid ProductVariantId { get; private set; }

    public Guid ProductAttributeId { get; private set; }

    public string Value { get; private set; }

    private ProductVariantAttributeValue(
        Guid productVariantId,
        Guid productAttributeId,
        string value)
    {
        ProductVariantId = productVariantId;
        ProductAttributeId = productAttributeId;
        Value = value;
    }

    public static ProductVariantAttributeValue Create(
        Guid productVariantId,
        Guid productAttributeId,
        string value)
    {
        if (productVariantId == Guid.Empty)
            throw new ArgumentException("Product variant ID is required.");

        if (productAttributeId == Guid.Empty)
            throw new ArgumentException("Product attribute ID is required.");

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Attribute value is required.");

        return new ProductVariantAttributeValue(
            productVariantId,
            productAttributeId,
            value);
    }

    public void UpdateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Attribute value is required.");

        Value = value;
    }
}