using CommerceFlow.Domain.Common;

namespace CommerceFlow.Domain.Catalog;

public class Brand : AggregateRoot
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public bool IsActive { get ; private set; }

    private Brand(string name, string slug)
    {
        Name = name;
        Slug = slug;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Brand Create(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Brand name is required.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Brand slug is required.");

        return new Brand(name, slug);
    }

    public void UpdateDetails(
        string name,
        string slug,
        string? description,
        string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Brand name is required.");

        Name = name;
        Slug = slug;
        Description = description;
        LogoUrl = logoUrl;
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