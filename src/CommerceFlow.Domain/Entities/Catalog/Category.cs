using CommerceFlow.Domain.Common;

namespace CommerceFlow.Domain.Catalog;

public class Category : AggregateRoot
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Description { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }
    
    private Category(string name,string slug)
    {
        Name = name;
        Slug = slug;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }
    public static Category Create(string name,string slug)
    {
        if(string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.");
        if(string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug name is required.");
        return new Category(name, slug);
    }
    public void UpdateDetails(string name,string slug,string? description,int displayOrder,Guid? parentCategoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.");
        if(string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Category Slug is required.");
        if (displayOrder < 0)
            throw new ArgumentException("Category Display-Order is negative");
        if (parentCategoryId == Id)
            throw new ArgumentException("Category parent must be not category itself");
        Name = name;
        Slug = slug;
        Description = description;
        DisplayOrder = displayOrder;
        ParentCategoryId = parentCategoryId;
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