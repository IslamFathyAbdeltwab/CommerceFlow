using CommerceFlow.Application.Catalog.Products.Commands.AddProductImage;
using CommerceFlow.Application.Catalog.Products.Commands.AddProductVariant;
using CommerceFlow.Application.Catalog.Products.Commands.ChangeVariantPrice;
using CommerceFlow.Application.Catalog.Products.Commands.CreateProduct;
using CommerceFlow.Application.Catalog.Products.Commands.SetVariantAttributeValue;
using CommerceFlow.Application.Catalog.Products.Commands.UpdateProduct;
using CommerceFlow.Application.Catalog.Products.Commands.UpdateProductVariant;
using CommerceFlow.Application.Catalog.Products.Queries.GetFeaturedProducts;
using CommerceFlow.Application.Catalog.Products.Queries.GetProducts;
using CommerceFlow.Domain.Entities.Catalog;

namespace CommerceFlow.Application.Tests.Catalog.Products;

public sealed class ProductValidatorTests
{
    // ---------- Create / Update product ----------

    [Fact]
    public void CreateProduct_WithValidInput_IsValid()
    {
        var result = new CreateProductCommandValidator()
            .Validate(new CreateProductCommand("iPhone 15", "iphone-15", "Best phone"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateProduct_WithEmptyName_IsInvalid()
    {
        var result = new CreateProductCommandValidator()
            .Validate(new CreateProductCommand("", "iphone-15", "Best phone"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Fact]
    public void CreateProduct_WithMalformedSlug_IsInvalid()
    {
        var result = new CreateProductCommandValidator()
            .Validate(new CreateProductCommand("iPhone", "iphone 15", "Best phone"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.Slug));
    }

    [Fact]
    public void CreateProduct_WithEmptyDescription_IsInvalid()
    {
        var result = new CreateProductCommandValidator()
            .Validate(new CreateProductCommand("iPhone", "iphone-15", ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.Description));
    }

    [Fact]
    public void CreateProduct_WithEmptyBrandId_IsInvalid()
    {
        var result = new CreateProductCommandValidator()
            .Validate(new CreateProductCommand("iPhone", "iphone-15", "Best", BrandId: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.BrandId));
    }

    [Fact]
    public void CreateProduct_WithEmptyCategoryId_IsInvalid()
    {
        var result = new CreateProductCommandValidator()
            .Validate(new CreateProductCommand(
                "iPhone", "iphone-15", "Best", CategoryIds: new List<Guid> { Guid.Empty }));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CategoryIds[0]");
    }

    [Fact]
    public void UpdateProduct_WithEmptyProductId_IsInvalid()
    {
        var result = new UpdateProductCommandValidator()
            .Validate(new UpdateProductCommand(Guid.Empty, "iPhone", "iphone-15", "Best", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProductCommand.ProductId));
    }

    [Fact]
    public void UpdateProduct_WithValidInput_IsValid()
    {
        var result = new UpdateProductCommandValidator()
            .Validate(new UpdateProductCommand(Guid.NewGuid(), "iPhone", "iphone-15", "Best", null));

        Assert.True(result.IsValid);
    }

    // ---------- Variants ----------

    [Fact]
    public void AddVariant_WithValidInput_IsValid()
    {
        var result = new AddProductVariantCommandValidator()
            .Validate(new AddProductVariantCommand(Guid.NewGuid(), "Base", "SKU-1", 100, 150));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void AddVariant_WithNegativePrice_IsInvalid()
    {
        var result = new AddProductVariantCommandValidator()
            .Validate(new AddProductVariantCommand(Guid.NewGuid(), "Base", "SKU-1", -1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddProductVariantCommand.Price));
    }

    [Fact]
    public void AddVariant_WithCompareAtPriceNotGreaterThanPrice_IsInvalid()
    {
        var result = new AddProductVariantCommandValidator()
            .Validate(new AddProductVariantCommand(Guid.NewGuid(), "Base", "SKU-1", 100, 100));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Compare-at price must be greater than the price.");
    }

    [Fact]
    public void AddVariant_WithEmptySku_IsInvalid()
    {
        var result = new AddProductVariantCommandValidator()
            .Validate(new AddProductVariantCommand(Guid.NewGuid(), "Base", "", 100));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddProductVariantCommand.Sku));
    }

    [Fact]
    public void ChangeVariantPrice_WithEmptyVariantId_IsInvalid()
    {
        var result = new ChangeVariantPriceCommandValidator()
            .Validate(new ChangeVariantPriceCommand(Guid.NewGuid(), Guid.Empty, 100));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangeVariantPriceCommand.VariantId));
    }

    [Fact]
    public void ChangeVariantPrice_WithValidInput_IsValid()
    {
        var result = new ChangeVariantPriceCommandValidator()
            .Validate(new ChangeVariantPriceCommand(Guid.NewGuid(), Guid.NewGuid(), 80, 120));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateVariant_WithNegativeWeight_IsInvalid()
    {
        var result = new UpdateProductVariantCommandValidator()
            .Validate(new UpdateProductVariantCommand(
                Guid.NewGuid(), Guid.NewGuid(), "Base", "SKU-1", 100, WeightGrams: -5));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProductVariantCommand.WeightGrams));
    }

    [Fact]
    public void UpdateVariant_WithBarcodeOverMaxLength_IsInvalid()
    {
        var result = new UpdateProductVariantCommandValidator()
            .Validate(new UpdateProductVariantCommand(
                Guid.NewGuid(), Guid.NewGuid(), "Base", "SKU-1", 100,
                Barcode: new string('1', ProductVariant.BarcodeMaxLength + 1)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateVariant_WithValidInput_IsValid()
    {
        var result = new UpdateProductVariantCommandValidator()
            .Validate(new UpdateProductVariantCommand(
                Guid.NewGuid(), Guid.NewGuid(), "Base", "SKU-1", 100, "123", 150, 300));

        Assert.True(result.IsValid);
    }

    // ---------- Images ----------

    [Fact]
    public void AddImage_WithValidInput_IsValid()
    {
        var result = new AddProductImageCommandValidator()
            .Validate(new AddProductImageCommand(Guid.NewGuid(), "img/a.jpg", "Front", 0));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void AddImage_WithEmptyStorageKey_IsInvalid()
    {
        var result = new AddProductImageCommandValidator()
            .Validate(new AddProductImageCommand(Guid.NewGuid(), "", "Front", 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddProductImageCommand.StorageKey));
    }

    [Fact]
    public void AddImage_WithNegativeDisplayOrder_IsInvalid()
    {
        var result = new AddProductImageCommandValidator()
            .Validate(new AddProductImageCommand(Guid.NewGuid(), "img/a.jpg", null, -1));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void AddImage_WithPrimaryFlagOnVariantImage_IsInvalid()
    {
        var result = new AddProductImageCommandValidator()
            .Validate(new AddProductImageCommand(
                Guid.NewGuid(), "img/a.jpg", null, 0, IsPrimary: true, VariantId: Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage == "The primary image must be a product-level image, not a variant image.");
    }

    // ---------- Variant attribute values ----------

    [Fact]
    public void SetVariantAttributeValue_WithValidInput_IsValid()
    {
        var result = new SetVariantAttributeValueCommandValidator()
            .Validate(new SetVariantAttributeValueCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Blue"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SetVariantAttributeValue_WithEmptyValue_IsInvalid()
    {
        var result = new SetVariantAttributeValueCommandValidator()
            .Validate(new SetVariantAttributeValueCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SetVariantAttributeValueCommand.Value));
    }

    [Fact]
    public void SetVariantAttributeValue_WithOverlongValue_IsInvalid()
    {
        var result = new SetVariantAttributeValueCommandValidator()
            .Validate(new SetVariantAttributeValueCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                new string('x', ProductVariantAttributeValue.ValueMaxLength + 1)));

        Assert.False(result.IsValid);
    }

    // ---------- Queries ----------

    [Fact]
    public void GetProducts_WithValidInput_IsValid()
    {
        var result = new GetProductsQueryValidator()
            .Validate(new GetProductsQuery(MinPrice: 10, MaxPrice: 100, PageSize: 50));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetProducts_WhenMaxPriceBelowMinPrice_IsInvalid()
    {
        var result = new GetProductsQueryValidator()
            .Validate(new GetProductsQuery(MinPrice: 100, MaxPrice: 50));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage == "MaxPrice must be greater than or equal to MinPrice.");
    }

    [Fact]
    public void GetProducts_WithOutOfRangePageSize_IsInvalid()
    {
        var result = new GetProductsQueryValidator().Validate(new GetProductsQuery(PageSize: 101));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetProductsQuery.PageSize));
    }

    [Fact]
    public void GetProducts_WithNegativePriceFilter_IsInvalid()
    {
        var result = new GetProductsQueryValidator().Validate(new GetProductsQuery(MinPrice: -5));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetProductsQuery.MinPrice));
    }

    [Fact]
    public void GetFeaturedProducts_WithDefaultTake_IsValid()
    {
        var result = new GetFeaturedProductsQueryValidator().Validate(new GetFeaturedProductsQuery());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void GetFeaturedProducts_WithOutOfRangeTake_IsInvalid(int take)
    {
        var result = new GetFeaturedProductsQueryValidator()
            .Validate(new GetFeaturedProductsQuery(take));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetFeaturedProductsQuery.Take));
    }
}
