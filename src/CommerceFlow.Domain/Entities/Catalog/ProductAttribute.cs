using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Domain.Catalog;

public class ProductAttribute : AggregateRoot
{
    public string Name { get; private set; }

    public string Code { get; private set; }

    public ProductAttributeDataType DataType { get; private set; }

    public bool IsVariantAttribute { get; private set; }

    public bool IsActive { get; private set; }

    public int DisplayOrder { get; private set; }

    private ProductAttribute(
        string name,
        string code,
        ProductAttributeDataType dataType,
        bool isVariantAttribute,
        int displayOrder)
    {
        Name = name;
        Code = code;
        DataType = dataType;
        IsVariantAttribute = isVariantAttribute;
        DisplayOrder = displayOrder;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static ProductAttribute Create(
        string name,
        string code,
        ProductAttributeDataType dataType,
        bool isVariantAttribute,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Attribute name is required.");

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Attribute code is required.");

        if (displayOrder < 0)
            throw new ArgumentException("Display order cannot be negative.");

        if (!Enum.IsDefined(dataType))
            throw new ArgumentException("Invalid attribute data type.");

        return new ProductAttribute(
            name,
            code,
            dataType,
            isVariantAttribute,
            displayOrder);
    }

    public void UpdateDetails(
        string name,
        string code,
        ProductAttributeDataType dataType,
        bool isVariantAttribute,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Attribute name is required.");

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Attribute code is required.");

        if (displayOrder < 0)
            throw new ArgumentException("Display order cannot be negative.");

        if (!Enum.IsDefined(dataType))
            throw new ArgumentException("Invalid attribute data type.");

        Name = name;
        Code = code;
        DataType = dataType;
        IsVariantAttribute = isVariantAttribute;
        DisplayOrder = displayOrder;
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
