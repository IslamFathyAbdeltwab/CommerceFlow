using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Enums;
namespace CommerceFlow.Domain.Catalog;

public class Product : AggregateRoot
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string Description { get; private set; }
    public Guid? BrandId { get; private set; }
    public ProductStatus Status { get; private set; }
    public bool IsFeatured { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }

    private Product(string name, string slug, string description, Guid? brandId)
    {
        Name = name;
        Slug = slug;
        Description = description;
        BrandId = brandId;
        Status = ProductStatus.Draft;
        IsFeatured = false;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Product Create(
        string name,
        string slug,
        string description,
        Guid? brandId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Product slug is required.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Product description is required.");

        return new Product(name, slug, description, brandId);
    }

    public void UpdateDetails(
        string name,
        string slug,
        string description,
        Guid? brandId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Product slug is required.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Product description is required.");

        Name = name;
        Slug = slug;
        Description = description;
        BrandId = brandId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Publish()
    {
        if (Status == ProductStatus.Published)
            throw new InvalidOperationException("Product is already published.");

        Status = ProductStatus.Published;
        PublishedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        if (Status == ProductStatus.Archived)
            throw new InvalidOperationException("Product is already archived.");

        Status = ProductStatus.Archived;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Feature()
    {
        IsFeatured = true;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Unfeature()
    {
        IsFeatured = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
