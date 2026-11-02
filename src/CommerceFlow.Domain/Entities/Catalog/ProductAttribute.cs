using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Catalog;

public class ProductAttribute : AggregateRoot
{
    public const int NameMaxLength = 100;
    public const int CodeMaxLength = 50;

    public string Name { get; private set; } = string.Empty;

    // Stable machine-readable key, "color", "storage".
    public string Code { get; private set; } = string.Empty;
    public ProductAttributeDataType DataType { get; private set; }

    // true = values of this attribute create separate variants (Color); false = just describes the product (Material).
    public bool IsVariantDefining { get; private set; }

    // true = the store can offer it as a filter on the product list.
    public bool IsFilterable { get; private set; }

    private ProductAttribute() { } // required by EF Core

    private ProductAttribute(
        string name,
        string code,
        ProductAttributeDataType dataType,
        bool isVariantDefining,
        bool isFilterable)
    {
        Name = name;
        Code = code;
        DataType = dataType;
        IsVariantDefining = isVariantDefining;
        IsFilterable = isFilterable;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static ProductAttribute Create(
        string name,
        string code,
        ProductAttributeDataType dataType,
        bool isVariantDefining,
        bool isFilterable)
    {
        var (n, c) = Validate(name, code, dataType);
        return new ProductAttribute(n, c, dataType, isVariantDefining, isFilterable);
    }

    public void UpdateDetails(
        string name,
        string code,
        ProductAttributeDataType dataType,
        bool isVariantDefining,
        bool isFilterable)
    {
        var (n, c) = Validate(name, code, dataType);

        Name = n;
        Code = c;
        DataType = dataType;
        IsVariantDefining = isVariantDefining;
        IsFilterable = isFilterable;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static (string Name, string Code) Validate(
        string? name, string? code, ProductAttributeDataType dataType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Attribute name is required.");
        name = name.Trim();
        if (name.Length > NameMaxLength)
            throw new DomainException($"Attribute name cannot exceed {NameMaxLength} characters.");

        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Attribute code is required.");
        code = code.Trim().ToLowerInvariant();
        if (code.Length > CodeMaxLength)
            throw new DomainException($"Attribute code cannot exceed {CodeMaxLength} characters.");
        if (code.Any(char.IsWhiteSpace))
            throw new DomainException("Attribute code cannot contain spaces.");

        if (!Enum.IsDefined(dataType))
            throw new DomainException("Invalid attribute data type.");

        return (name, code);
    }
}