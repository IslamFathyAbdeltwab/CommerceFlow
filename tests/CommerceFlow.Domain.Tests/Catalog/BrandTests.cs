using Xunit;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Catalog;

public class BrandTests
{
    private static Brand NewBrand() =>
        Brand.Create("Apple", "apple");

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_StartsActiveWithNormalizedValues()
    {
        var brand = Brand.Create("  Apple ", " APPLE ", "  Consumer electronics ", " logos/apple.png ");

        Assert.Equal("Apple", brand.Name);
        Assert.Equal("apple", brand.Slug);
        Assert.Equal("Consumer electronics", brand.Description);
        Assert.Equal("logos/apple.png", brand.LogoUrl);
        Assert.True(brand.IsActive);
    }

    [Theory]
    [InlineData("", "apple")]
    [InlineData("   ", "apple")]
    [InlineData("Apple", "")]
    [InlineData("Apple", "   ")]
    public void Create_EmptyNameOrSlug_Throws(string name, string slug)
    {
        Assert.Throws<DomainException>(() => Brand.Create(name, slug));
    }

    [Fact]
    public void Create_SlugWithSpaces_Throws()
    {
        Assert.Throws<DomainException>(() => Brand.Create("Apple", "apple inc"));
    }

    [Fact]
    public void Create_NameLongerThanMaximum_Throws()
    {
        var tooLong = new string('a', Brand.NameMaxLength + 1);

        Assert.Throws<DomainException>(() => Brand.Create(tooLong, "apple"));
    }

    [Fact]
    public void Create_BlankDescriptionAndLogo_AreStoredAsNull()
    {
        var brand = Brand.Create("Apple", "apple", "   ", "   ");

        Assert.Null(brand.Description);
        Assert.Null(brand.LogoUrl);
    }

    // ---------- UpdateDetails ----------

    [Fact]
    public void UpdateDetails_ValidData_UpdatesAllFields()
    {
        var brand = NewBrand();

        brand.UpdateDetails("Samsung", "Samsung", "Korean electronics", "logos/samsung.png");

        Assert.Equal("Samsung", brand.Name);
        Assert.Equal("samsung", brand.Slug);
        Assert.Equal("Korean electronics", brand.Description);
        Assert.Equal("logos/samsung.png", brand.LogoUrl);
    }

    [Fact]
    public void UpdateDetails_EmptySlug_Throws()
    {
        var brand = NewBrand();

        Assert.Throws<DomainException>(() =>
            brand.UpdateDetails("Apple", "", null, null));
    }

    [Fact]
    public void UpdateDetails_InvalidData_LeavesBrandUnchanged()
    {
        var brand = NewBrand();

        Assert.Throws<DomainException>(() =>
            brand.UpdateDetails("", "samsung", null, null));

        Assert.Equal("Apple", brand.Name);
        Assert.Equal("apple", brand.Slug);
    }

    // ---------- Activate / Deactivate ----------

    [Fact]
    public void Deactivate_ActiveBrand_BecomesInactive()
    {
        var brand = NewBrand();

        brand.Deactivate();

        Assert.False(brand.IsActive);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_DoesNotChangeUpdatedAt()
    {
        var brand = NewBrand();
        brand.Deactivate();
        var updatedAfterFirstCall = brand.UpdatedAtUtc;

        brand.Deactivate();

        Assert.Equal(updatedAfterFirstCall, brand.UpdatedAtUtc);
    }

    [Fact]
    public void Activate_InactiveBrand_BecomesActive()
    {
        var brand = NewBrand();
        brand.Deactivate();

        brand.Activate();

        Assert.True(brand.IsActive);
    }
}