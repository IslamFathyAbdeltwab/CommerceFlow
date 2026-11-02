using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Catalog;

public class Brand : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int SlugMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int LogoUrlMaxLength = 500;

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public bool IsActive { get; private set; }

    private Brand() { } // required by EF Core

    private Brand(string name, string slug, string? description, string? logoUrl)
    {
        Name = name;
        Slug = slug;
        Description = description;
        LogoUrl = logoUrl;
        IsActive = true;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Brand Create(
        string name,
        string slug,
        string? description = null,
        string? logoUrl = null)
    {
        var (n, s, d, l) = Validate(name, slug, description, logoUrl);
        return new Brand(n, s, d, l);
    }

    public void UpdateDetails(
        string name,
        string slug,
        string? description,
        string? logoUrl)
    {
        var (n, s, d, l) = Validate(name, slug, description, logoUrl);

        Name = n;
        Slug = s;
        Description = d;
        LogoUrl = l;
        Touch();
    }

    public void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static (string Name, string Slug, string? Description, string? LogoUrl) Validate(
        string? name, string? slug, string? description, string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Brand name is required.");
        name = name.Trim();
        if (name.Length > NameMaxLength)
            throw new DomainException($"Brand name cannot exceed {NameMaxLength} characters.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Brand slug is required.");
        slug = slug.Trim().ToLowerInvariant();
        if (slug.Length > SlugMaxLength)
            throw new DomainException($"Brand slug cannot exceed {SlugMaxLength} characters.");
        if (slug.Any(char.IsWhiteSpace))
            throw new DomainException("Brand slug cannot contain spaces.");

        var desc = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (desc is { Length: > DescriptionMaxLength })
            throw new DomainException($"Brand description cannot exceed {DescriptionMaxLength} characters.");

        var logo = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
        if (logo is { Length: > LogoUrlMaxLength })
            throw new DomainException($"Brand logo URL cannot exceed {LogoUrlMaxLength} characters.");

        return (name, slug, desc, logo);
    }
}