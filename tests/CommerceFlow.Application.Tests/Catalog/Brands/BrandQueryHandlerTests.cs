using CommerceFlow.Application.Catalog.Brands.Queries.GetBrandById;
using CommerceFlow.Application.Catalog.Brands.Queries.GetBrandBySlug;
using CommerceFlow.Application.Catalog.Brands.Queries.GetBrands;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Brands;

public sealed class BrandQueryHandlerTests
{
    private readonly Mock<IBrandRepository> _brands = new();

    [Fact]
    public async Task GetBrandById_WhenBrandDoesNotExist_ReturnsNull()
    {
        var handler = new GetBrandByIdQueryHandler(_brands.Object);
        _brands
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        var result = await handler.Handle(new GetBrandByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBrandById_WhenBrandExists_MapsAllFields()
    {
        var handler = new GetBrandByIdQueryHandler(_brands.Object);
        var brand = Brand.Create("Apple", "apple", "Smartphones", "https://logo.png");
        brand.Deactivate();
        _brands
            .Setup(x => x.GetByIdAsync(brand.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(brand);

        var result = await handler.Handle(new GetBrandByIdQuery(brand.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(brand.Id, result.Id);
        Assert.Equal("Apple", result.Name);
        Assert.Equal("apple", result.Slug);
        Assert.Equal("Smartphones", result.Description);
        Assert.Equal("https://logo.png", result.LogoUrl);
        Assert.False(result.IsActive);
        Assert.Equal(brand.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(brand.UpdatedAtUtc, result.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetBrandBySlug_NormalizesSlugBeforeLookup()
    {
        var handler = new GetBrandBySlugQueryHandler(_brands.Object);
        var brand = Brand.Create("Apple", "apple");
        _brands
            .Setup(x => x.GetBySlugAsync("apple", It.IsAny<CancellationToken>()))
            .ReturnsAsync(brand);

        var result = await handler.Handle(new GetBrandBySlugQuery("  Apple  "), CancellationToken.None);

        Assert.NotNull(result);
        _brands.Verify(x => x.GetBySlugAsync("apple", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBrandBySlug_WhenBrandDoesNotExist_ReturnsNull()
    {
        var handler = new GetBrandBySlugQueryHandler(_brands.Object);
        _brands
            .Setup(x => x.GetBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        var result = await handler.Handle(new GetBrandBySlugQuery("missing"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBrands_ReturnsPagedResultWithMappedItems()
    {
        var handler = new GetBrandsQueryHandler(_brands.Object);
        var first = Brand.Create("Apple", "apple");
        var second = Brand.Create("Samsung", "samsung");
        second.Deactivate();
        _brands
            .Setup(x => x.CountAsync("phone", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        _brands
            .Setup(x => x.GetAllAsync("phone", true, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Brand> { first, second });

        var result = await handler.Handle(
            new GetBrandsQuery(" phone ", true, PageNumber: 2, PageSize: 10),
            CancellationToken.None);

        Assert.Equal(42, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(5, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Apple", result.Items[0].Name);
        Assert.True(result.Items[0].IsActive);
        Assert.False(result.Items[1].IsActive);
    }
}
