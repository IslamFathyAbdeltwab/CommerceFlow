using Xunit;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Catalog;

public class CategoryTests
{
    private static Category NewCategory() =>
        Category.Create("Phones", "phones");

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_StartsActiveWithNormalizedValues()
    {
        var category = Category.Create("  Phones ", " Phones-And-Tablets ", "  Mobile devices ", 2);

        Assert.Equal("Phones", category.Name);
        Assert.Equal("phones-and-tablets", category.Slug);
        Assert.Equal("Mobile devices", category.Description);
        Assert.Equal(2, category.DisplayOrder);
        Assert.True(category.IsActive);
        Assert.Null(category.ParentCategoryId);
    }

    [Theory]
    [InlineData("", "phones")]
    [InlineData("   ", "phones")]
    [InlineData("Phones", "")]
    [InlineData("Phones", "   ")]
    public void Create_EmptyNameOrSlug_Throws(string name, string slug)
    {
        Assert.Throws<DomainException>(() => Category.Create(name, slug));
    }

    [Fact]
    public void Create_SlugWithSpaces_Throws()
    {
        Assert.Throws<DomainException>(() => Category.Create("Phones", "mobile phones"));
    }

    [Fact]
    public void Create_NameLongerThanMaximum_Throws()
    {
        var tooLong = new string('a', Category.NameMaxLength + 1);

        Assert.Throws<DomainException>(() => Category.Create(tooLong, "phones"));
    }

    [Fact]
    public void Create_NegativeDisplayOrder_Throws()
    {
        Assert.Throws<DomainException>(() => Category.Create("Phones", "phones", displayOrder: -1));
    }

    [Fact]
    public void Create_EmptyParentGuid_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Category.Create("Phones", "phones", parentCategoryId: Guid.Empty));
    }

    [Fact]
    public void Create_WithParent_KeepsParentId()
    {
        var parentId = Guid.NewGuid();

        var category = Category.Create("Phones", "phones", parentCategoryId: parentId);

        Assert.Equal(parentId, category.ParentCategoryId);
    }

    // ---------- UpdateDetails ----------

    [Fact]
    public void UpdateDetails_ParentIsItself_Throws()
    {
        var category = NewCategory();

        Assert.Throws<DomainException>(() =>
            category.UpdateDetails("Phones", "phones", null, 0, category.Id));
    }

    [Fact]
    public void UpdateDetails_ValidParent_SetsParent()
    {
        var category = NewCategory();
        var parentId = Guid.NewGuid();

        category.UpdateDetails("Smartphones", "smartphones", "All smartphones", 1, parentId);

        Assert.Equal("Smartphones", category.Name);
        Assert.Equal("smartphones", category.Slug);
        Assert.Equal(parentId, category.ParentCategoryId);
    }

    [Fact]
    public void UpdateDetails_InvalidData_LeavesCategoryUnchanged()
    {
        var category = NewCategory();

        Assert.Throws<DomainException>(() =>
            category.UpdateDetails("", "smartphones", null, 0, null));

        Assert.Equal("Phones", category.Name);
        Assert.Equal("phones", category.Slug);
    }

    [Fact]
    public void UpdateDetails_RemovingParent_SetsParentToNull()
    {
        var category = Category.Create("Phones", "phones", parentCategoryId: Guid.NewGuid());

        category.UpdateDetails("Phones", "phones", null, 0, null);

        Assert.Null(category.ParentCategoryId);
    }

    // ---------- Activate / Deactivate ----------

    [Fact]
    public void Deactivate_ActiveCategory_BecomesInactive()
    {
        var category = NewCategory();

        category.Deactivate();

        Assert.False(category.IsActive);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_DoesNotChangeUpdatedAt()
    {
        var category = NewCategory();
        category.Deactivate();
        var updatedAfterFirstCall = category.UpdatedAtUtc;

        category.Deactivate();

        Assert.Equal(updatedAfterFirstCall, category.UpdatedAtUtc);
    }

    [Fact]
    public void Activate_InactiveCategory_BecomesActive()
    {
        var category = NewCategory();
        category.Deactivate();

        category.Activate();

        Assert.True(category.IsActive);
    }
}