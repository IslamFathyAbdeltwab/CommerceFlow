using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;
namespace CommerceFlow.Domain.Tests.Catalog;

public class ProductTests
{
    // ---------- Helpers ----------

    private static Product NewDraftProduct() =>
        Product.Create("iPhone 15", "iphone-15", "Apple smartphone", null);

    private static (Product Product, Guid VariantId) NewPublishedProduct()
    {
        var product = NewDraftProduct();
        var variantId = product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m);
        product.Publish();
        return (product, variantId);
    }

    // ---------- Create ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_Throws(string name)
    {
        Assert.Throws<DomainException>(() =>
            Product.Create(name, "iphone-15", "Apple smartphone", null));
    }

    [Fact]
    public void Create_NormalizesSlugAndStartsAsDraft()
    {
        var product = Product.Create("  iPhone 15 ", " IPhone-15 ", "Apple smartphone", null);

        Assert.Equal("iPhone 15", product.Name);
        Assert.Equal("iphone-15", product.Slug);
        Assert.Equal(ProductStatus.Draft, product.Status);
    }

    [Fact]
    public void Create_WithSpaceInSlug_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Product.Create("iPhone 15", "iphone 15", "Apple smartphone", null));
    }

    // ---------- Publish ----------

    [Fact]
    public void Publish_WithoutVariants_Throws()
    {
        var product = NewDraftProduct();

        Assert.Throws<DomainException>(() => product.Publish());
    }

    [Fact]
    public void Publish_WithActiveVariant_SetsStatusAndPublishedDate()
    {
        var (product, _) = NewPublishedProduct();

        Assert.Equal(ProductStatus.Published, product.Status);
        Assert.NotNull(product.PublishedAtUtc);
    }

    [Fact]
    public void Feature_OnDraftProduct_Throws()
    {
        var product = NewDraftProduct();

        Assert.Throws<DomainException>(() => product.Feature());
    }

    // ---------- Variants ----------

    [Fact]
    public void AddVariant_WithDuplicateSkuInDifferentCase_Throws()
    {
        var product = NewDraftProduct();
        product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m);

        Assert.Throws<DomainException>(() =>
            product.AddVariant("Black copy", "ip15-blk-128", 36000m));
    }

    [Fact]
    public void DeactivateVariant_LastActiveVariantOfPublishedProduct_Throws()
    {
        var (product, variantId) = NewPublishedProduct();

        Assert.Throws<DomainException>(() => product.DeactivateVariant(variantId));
    }

    [Fact]
    public void RemoveVariant_LastActiveVariantOfPublishedProduct_Throws()
    {
        var (product, variantId) = NewPublishedProduct();

        Assert.Throws<DomainException>(() => product.RemoveVariant(variantId));
    }

    [Fact]
    public void RemoveVariant_AlsoRemovesImagesTaggedToIt()
    {
        var product = NewDraftProduct();
        var variantId = product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m);
        product.AddImage("products/front.jpg", "Front", 0);
        product.AddImage("products/black.jpg", "Black", 1, variantId: variantId);

        product.RemoveVariant(variantId);

        Assert.Single(product.Images);
    }

    // ---------- Images ----------

    [Fact]
    public void AddImage_FirstProductImage_BecomesPrimary()
    {
        var product = NewDraftProduct();

        product.AddImage("products/front.jpg", "Front", 0);

        Assert.True(product.Images.Single().IsPrimary);
    }

    [Fact]
    public void AddImage_NewPrimary_UnmarksThePreviousOne()
    {
        var product = NewDraftProduct();
        var firstId = product.AddImage("products/front.jpg", "Front", 0);

        var secondId = product.AddImage("products/back.jpg", "Back", 1, isPrimary: true);

        Assert.False(product.Images.Single(i => i.Id == firstId).IsPrimary);
        Assert.True(product.Images.Single(i => i.Id == secondId).IsPrimary);
        Assert.Single(product.Images, i => i.IsPrimary);
    }

    [Fact]
    public void AddImage_PrimaryImageTaggedToVariant_Throws()
    {
        var product = NewDraftProduct();
        var variantId = product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m);

        Assert.Throws<DomainException>(() =>
            product.AddImage("products/black.jpg", "Black", 0, isPrimary: true, variantId: variantId));
    }

    [Fact]
    public void RemoveImage_Primary_PromotesNextProductLevelImage()
    {
        var product = NewDraftProduct();
        var firstId = product.AddImage("products/front.jpg", "Front", 0);
        var secondId = product.AddImage("products/back.jpg", "Back", 1);

        product.RemoveImage(firstId);

        Assert.True(product.Images.Single(i => i.Id == secondId).IsPrimary);
    }

    // ---------- Archived ----------

    [Fact]
    public void AddVariant_OnArchivedProduct_Throws()
    {
        var product = NewDraftProduct();
        product.Archive();

        Assert.Throws<DomainException>(() =>
            product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m));
    }

    [Fact]
    public void Archive_UnfeaturesTheProduct()
    {
        var (product, _) = NewPublishedProduct();
        product.Feature();

        product.Archive();

        Assert.False(product.IsFeatured);
        Assert.Equal(ProductStatus.Archived, product.Status);
    }
}