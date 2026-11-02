using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Catalog;

public class ProductVariantAttributeValue
{
    public const int ValueMaxLength = 200;

    public Guid ProductVariantId { get; private set; }
    public Guid ProductAttributeId { get; private set; }
    public string Value { get; private set; } = string.Empty;

    private ProductVariantAttributeValue() { } // required by EF Core

    // internal: only ProductVariant (inside the Product aggregate) creates it.
    internal ProductVariantAttributeValue(Guid productVariantId, Guid productAttributeId, string value)
    {
        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant ID is required.");

        if (productAttributeId == Guid.Empty)
            throw new DomainException("Product attribute ID is required.");

        ProductVariantId = productVariantId;
        ProductAttributeId = productAttributeId;
        Value = ValidateValue(value);
    }

    internal void ChangeValue(string value) => Value = ValidateValue(value);

    private static string ValidateValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Attribute value is required.");

        value = value.Trim();
        if (value.Length > ValueMaxLength)
            throw new DomainException($"Attribute value cannot exceed {ValueMaxLength} characters.");

        return value;
    }
}