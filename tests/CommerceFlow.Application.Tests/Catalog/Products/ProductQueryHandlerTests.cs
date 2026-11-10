using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Products.Queries.GetFeaturedProducts;
using CommerceFlow.Application.Catalog.Products.Queries.GetProductById;
using CommerceFlow.Application.Catalog.Products.Queries.GetProductBySlug;
using CommerceFlow.Application.Catalog.Products.Queries.GetProductStats;
using CommerceFlow.Application.Catalog.Products.Queries.GetProducts;
using CommerceFlow.Application.Catalog.Products.Queries.GetProductVariants;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Products;

public sealed class ProductQueryHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();

    /// <summary>
    /// Product with 3 variants (cheapest one deactivated), 2 images where the
    /// second one was promoted to primary, and one category link.
    /// </summary>
    private static Product CreateRichProduct(Guid? brandId = null)
    {
        var product = Product.Create("iPhone 15", "iphone-15", "Best phone", brandId);
        product.AddCategory(Guid.NewGuid());
        product.AddVariant("Base", "SKU-1", 100);
        product.AddVariant("Pro", "SKU-2", 150);
        product.AddVariant("Legacy", "SKU-3", 50);
        product.DeactivateVariant(product.Variants.Single(v => v.Sku == "SKU-3").Id);
        product.SetVariantAttributeValue(
            product.Variants.Single(v => v.Sku == "SKU-1").Id, Guid.NewGuid(), "Blue");

        product.AddImage("img/a.jpg", "First", 0); // auto primary
        product.AddImage("img/b.jpg", "Second", 1);
        product.SetPrimaryImage(product.Images.Single(i => i.StorageKey == "img/b.jpg").Id);

        return product;
    }

    // ---------- GetProductById ----------

    [Fact]
    public async Task GetProductById_WhenProductDoesNotExist_ReturnsNull()
    {
        var handler = new GetProductByIdQueryHandler(_products.Object);
        _products
            .Setup(x => x.GetProductByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await handler.Handle(new GetProductByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductById_MapsAggregateIncludingVariantsImagesAndPrimaryImage()
    {
        var handler = new GetProductByIdQueryHandler(_products.Object);
        var brandId = Guid.NewGuid();
        var product = CreateRichProduct(brandId);
        _products
            .Setup(x => x.GetProductByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal("iPhone 15", result.Name);
        Assert.Equal("iphone-15", result.Slug);
        Assert.Equal(brandId, result.BrandId);
        Assert.Equal(ProductStatus.Draft, result.Status);
        Assert.False(result.IsFeatured);
        Assert.Null(result.PublishedAtUtc);

        Assert.Single(result.CategoryIds);
        Assert.Equal(3, result.Variants.Count);
        Assert.Equal(2, result.Images.Count);
        // The promoted image wins even though it has the higher display order.
        Assert.Equal("img/b.jpg", result.PrimaryImageStorageKey);

        var baseVariant = result.Variants.Single(v => v.Sku == "SKU-1");
        var attribute = Assert.Single(baseVariant.Attributes);
        Assert.Equal("Blue", attribute.Value);
        Assert.False(result.Variants.Single(v => v.Sku == "SKU-3").IsActive);
    }

    // ---------- GetProductBySlug ----------

    [Fact]
    public async Task GetProductBySlug_NormalizesSlugBeforeLookup()
    {
        var handler = new GetProductBySlugQueryHandler(_products.Object);
        var product = CreateRichProduct();
        _products
            .Setup(x => x.GetProductBySlugAsync("iphone-15", It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await handler.Handle(new GetProductBySlugQuery(" iPhone-15 "), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        _products.Verify(
            x => x.GetProductBySlugAsync("iphone-15", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProductBySlug_WhenProductDoesNotExist_ReturnsNull()
    {
        var handler = new GetProductBySlugQueryHandler(_products.Object);
        _products
            .Setup(x => x.GetProductBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await handler.Handle(new GetProductBySlugQuery("missing"), CancellationToken.None);

        Assert.Null(result);
    }

    // ---------- GetProducts ----------

    [Fact]
    public async Task GetProducts_ReturnsPagedResultWithDerivedPricesAndPrimaryImage()
    {
        var handler = new GetProductsQueryHandler(_products.Object);
        var brandId = Guid.NewGuid();
        var product = CreateRichProduct(brandId);
        _products
            .Setup(x => x.CountPublishedProductsAsync(null, brandId, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);
        _products
            .Setup(x => x.GetPublishedProductsAsync(null, brandId, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var result = await handler.Handle(new GetProductsQuery(BrandId: brandId), CancellationToken.None);

        Assert.Equal(7, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);

        var item = Assert.Single(result.Items);
        Assert.Equal(product.Id, item.Id);
        Assert.Equal(brandId, item.BrandId);
        // Cheapest active variant only: the deactivated 50 must be ignored.
        Assert.Equal(100m, item.MinPrice);
        Assert.Equal(100m, item.Price);
        Assert.Equal("img/b.jpg", item.PrimaryImageStorageKey);
        Assert.Single(item.CategoryIds);
    }

    [Fact]
    public async Task GetProducts_TrimsSearchTermBeforeQuerying()
    {
        var handler = new GetProductsQueryHandler(_products.Object);
        _products
            .Setup(x => x.CountPublishedProductsAsync("phone", null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _products
            .Setup(x => x.GetPublishedProductsAsync("phone", null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var result = await handler.Handle(new GetProductsQuery(SearchTerm: " phone "), CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    // ---------- GetFeaturedProducts ----------

    [Fact]
    public async Task GetFeaturedProducts_PassesTakeThroughAndMapsItems()
    {
        var handler = new GetFeaturedProductsQueryHandler(_products.Object);
        var product = CreateRichProduct();
        product.Publish();
        product.Feature();
        _products
            .Setup(x => x.GetFeaturedProductsAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var result = await handler.Handle(new GetFeaturedProductsQuery(Take: 5), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.True(item.IsFeatured);
        Assert.NotNull(item.PublishedAtUtc);
        Assert.Equal(100m, item.MinPrice);
        Assert.Equal("img/b.jpg", item.PrimaryImageStorageKey);
        _products.Verify(x => x.GetFeaturedProductsAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- GetProductStats ----------

    [Fact]
    public async Task GetProductStats_ReturnsCountsPerStatusWithTotal()
    {
        var handler = new GetProductStatsQueryHandler(_products.Object);
        _products
            .Setup(x => x.CountByStatusAsync(ProductStatus.Draft, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _products
            .Setup(x => x.CountByStatusAsync(ProductStatus.Published, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _products
            .Setup(x => x.CountByStatusAsync(ProductStatus.Archived, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var result = await handler.Handle(new GetProductStatsQuery(), CancellationToken.None);

        Assert.Equal(3, result.DraftCount);
        Assert.Equal(5, result.PublishedCount);
        Assert.Equal(2, result.ArchivedCount);
        Assert.Equal(10, result.TotalCount);
    }

    // ---------- GetProductVariants ----------

    [Fact]
    public async Task GetProductVariants_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new GetProductVariantsQueryHandler(_products.Object);
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetProductVariantsQuery(productId), CancellationToken.None));

        Assert.Contains("Product", exception.Message);
    }

    [Fact]
    public async Task GetProductVariants_MapsVariantsWithAttributes()
    {
        var handler = new GetProductVariantsQueryHandler(_products.Object);
        var product = CreateRichProduct();
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await handler.Handle(new GetProductVariantsQuery(product.Id), CancellationToken.None);

        Assert.Equal(3, result.Count);
        var baseVariant = result.Single(v => v.Sku == "SKU-1");
        Assert.Equal(100m, baseVariant.Price);
        Assert.True(baseVariant.IsActive);
        var attribute = Assert.Single(baseVariant.Attributes);
        Assert.Equal("Blue", attribute.Value);
        Assert.False(result.Single(v => v.Sku == "SKU-3").IsActive);
    }
}
