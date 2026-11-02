using Xunit;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Catalog;

// ProductVariant's mutators are internal, so every test goes through the Product root,
// exactly like real callers do.
public class ProductVariantTests
{
    private static Product NewProduct() =>
        Product.Create("iPhone 15", "iphone-15", "Apple smartphone", null);

    private static ProductVariant GetVariant(Product product, Guid variantId) =>
        product.Variants.Single(v => v.Id == variantId);

    // ---------- Creation (AddVariant) ----------

    [Fact]
    public void AddVariant_ValidData_CreatesActiveVariantLinkedToProduct()
    {
        var product = NewProduct();

        var variantId = product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m);

        var variant = GetVariant(product, variantId);
        Assert.Equal(product.Id, variant.ProductId);
        Assert.True(variant.IsActive);
        Assert.Equal(35000m, variant.Price);
    }

    [Fact]
    public void AddVariant_TrimsNameAndNormalizesSku()
    {
        var product = NewProduct();

        var variantId = product.AddVariant("  Black / 128 GB ", "  ip15-blk-128 ", 35000m);

        var variant = GetVariant(product, variantId);
        Assert.Equal("Black / 128 GB", variant.Name);
        Assert.Equal("IP15-BLK-128", variant.Sku);
    }

    [Theory]
    [InlineData("", "IP15-BLK-128")]
    [InlineData("   ", "IP15-BLK-128")]
    [InlineData("Black / 128 GB", "")]
    [InlineData("Black / 128 GB", "   ")]
    public void AddVariant_EmptyNameOrSku_Throws(string name, string sku)
    {
        var product = NewProduct();

        Assert.Throws<DomainException>(() => product.AddVariant(name, sku, 35000m));
    }

    [Fact]
    public void AddVariant_NegativePrice_Throws()
    {
        var product = NewProduct();

        Assert.Throws<DomainException>(() =>
            product.AddVariant("Black / 128 GB", "IP15-BLK-128", -1m));
    }

    [Fact]
    public void AddVariant_ZeroPrice_IsAllowed()
    {
        var product = NewProduct();

        var variantId = product.AddVariant("Free sample", "SAMPLE-1", 0m);

        Assert.Equal(0m, GetVariant(product, variantId).Price);
    }

    [Theory]
    [InlineData(35000)] // equal to price
    [InlineData(30000)] // lower than price
    public void AddVariant_CompareAtPriceNotGreaterThanPrice_Throws(double compareAtPrice)
    {
        var product = NewProduct();

        Assert.Throws<DomainException>(() =>
            product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m, (decimal)compareAtPrice));
    }

    [Fact]
    public void AddVariant_CompareAtPriceGreaterThanPrice_IsAccepted()
    {
        var product = NewProduct();

        var variantId = product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m, 40000m);

        Assert.Equal(40000m, GetVariant(product, variantId).CompareAtPrice);
    }

    // ---------- ChangeVariantPrice ----------

    [Fact]
    public void ChangeVariantPrice_ValidValues_UpdatesPrice()
    {
        var product = NewProduct();
        var variantId = product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m);

        product.ChangeVariantPrice(variantId, 33000m, 38000m);

        var variant = GetVariant(product, variantId);
        Assert.Equal(33000m, variant.Price);
        Assert.Equal(38000m, variant.CompareAtPrice);
    }

    [Fact]
    public void ChangeVariantPrice_NegativePrice_Throws()
    {
        var product = NewProduct();
        var variantId = product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m);

        Assert.Throws<DomainException>(() => product.ChangeVariantPrice(variantId, -5m));
    }

    [Fact]
    public void ChangeVariantPrice_InvalidValues_LeavesPriceUnchanged()
    {
        var product = NewProduct();
        var variantId = product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m);

        Assert.Throws<DomainException>(() => product.ChangeVariantPrice(variantId, -5m));

        Assert.Equal(35000m, GetVariant(product, variantId).Price);
    }

    [Fact]
    public void ChangeVariantPrice_UnknownVariant_Throws()
    {
        var product = NewProduct();

        Assert.Throws<DomainException>(() => product.ChangeVariantPrice(Guid.NewGuid(), 100m));
    }

    // ---------- Activate / Deactivate ----------

    [Fact]
    public void DeactivateVariant_AlreadyInactive_DoesNotChangeUpdatedAt()
    {
        var product = NewProduct();
        var variantId = product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m);
        product.AddVariant("Blue / 256 GB", "IP15-BLU-256", 40000m);
        product.DeactivateVariant(variantId);
        var updatedAfterFirstCall = GetVariant(product, variantId).UpdatedAtUtc;

        product.DeactivateVariant(variantId);

        Assert.Equal(updatedAfterFirstCall, GetVariant(product, variantId).UpdatedAtUtc);
    }

    [Fact]
    public void ActivateVariant_InactiveVariant_BecomesActive()
    {
        var product = NewProduct();
        var variantId = product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m);
        product.DeactivateVariant(variantId);

        product.ActivateVariant(variantId);

        Assert.True(GetVariant(product, variantId).IsActive);
    }

    // ---------- UpdateVariantDetails ----------

    [Fact]
    public void UpdateVariantDetails_NegativeWeight_Throws()
    {
        var product = NewProduct();
        var variantId = product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m);

        Assert.Throws<DomainException>(() =>
            product.UpdateVariantDetails(variantId, "Black / 128 GB", "IP15-BLK-128", null, 35000m, null, -10));
    }

    [Fact]
    public void UpdateVariantDetails_SkuUsedByAnotherVariant_Throws()
    {
        var product = NewProduct();
        var firstId = product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m);
        product.AddVariant("Blue / 256 GB", "IP15-BLU-256", 40000m);

        Assert.Throws<DomainException>(() =>
            product.UpdateVariantDetails(firstId, "Black / 128 GB", "ip15-blu-256", null, 35000m, null, null));
    }

    [Fact]
    public void UpdateVariantDetails_KeepingItsOwnSku_Succeeds()
    {
        var product = NewProduct();
        var variantId = product.AddVariant("Black / 128 GB", "IP15-BLK-128", 35000m);

        product.UpdateVariantDetails(variantId, "Black 128", "ip15-blk-128", "123456", 34000m, null, 187);

        var variant = GetVariant(product, variantId);
        Assert.Equal("Black 128", variant.Name);
        Assert.Equal("IP15-BLK-128", variant.Sku);
        Assert.Equal("123456", variant.Barcode);
        Assert.Equal(187, variant.WeightGrams);
    }
}