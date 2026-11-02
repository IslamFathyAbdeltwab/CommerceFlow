using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Catalog;

public class Category : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int SlugMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    // Self-reference: the parent is another Category aggregate, so we keep only its ID.
    public Guid? ParentCategoryId { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }

    private Category() { } // required by EF Core

    private Category(
        string name,
        string slug,
        string? description,
        int displayOrder,
        Guid? parentCategoryId)
    {
        Name = name;
        Slug = slug;
        Description = description;
        DisplayOrder = displayOrder;
        ParentCategoryId = parentCategoryId;
        IsActive = true;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Category Create(
        string name,
        string slug,
        string? description = null,
        int displayOrder = 0,
        Guid? parentCategoryId = null)
    {
        var (n, s, d) = Validate(name, slug, description, displayOrder, parentCategoryId);
        return new Category(n, s, d, displayOrder, parentCategoryId);
    }

    public void UpdateDetails(
        string name,
        string slug,
        string? description,
        int displayOrder,
        Guid? parentCategoryId)
    {
        var (n, s, d) = Validate(name, slug, description, displayOrder, parentCategoryId);

        if (parentCategoryId == Id)
            throw new DomainException("A category cannot be its own parent.");

        Name = n;
        Slug = s;
        Description = d;
        DisplayOrder = displayOrder;
        ParentCategoryId = parentCategoryId;
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

    private static (string Name, string Slug, string? Description) Validate(
        string? name, string? slug, string? description, int displayOrder, Guid? parentCategoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required.");
        name = name.Trim();
        if (name.Length > NameMaxLength)
            throw new DomainException($"Category name cannot exceed {NameMaxLength} characters.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Category slug is required.");
        slug = slug.Trim().ToLowerInvariant();
        if (slug.Length > SlugMaxLength)
            throw new DomainException($"Category slug cannot exceed {SlugMaxLength} characters.");
        if (slug.Any(char.IsWhiteSpace))
            throw new DomainException("Category slug cannot contain spaces.");

        if (displayOrder < 0)
            throw new DomainException("Category display order cannot be negative.");

        if (parentCategoryId == Guid.Empty)
            throw new DomainException("Parent category ID cannot be empty.");

        var desc = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (desc is { Length: > DescriptionMaxLength })
            throw new DomainException($"Category description cannot exceed {DescriptionMaxLength} characters.");

        return (name, slug, desc);
    }
}