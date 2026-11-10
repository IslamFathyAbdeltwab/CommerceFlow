using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Products.Commands.CreateProduct;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Products;

public sealed class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IBrandRepository> _brands = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateProductCommandHandler CreateHandler() =>
        new(_products.Object, _brands.Object, _categories.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WhenSlugIsFree_AddsProductWithBrandAndCategories()
    {
        var handler = CreateHandler();
        var brandId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        _products
            .Setup(x => x.SlugExistsAsync("iphone-15", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _brands
            .Setup(x => x.ExistsAsync(brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _categories
            .Setup(x => x.AllExistAsync(It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(categoryId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var id = await handler.Handle(
            new CreateProductCommand("iPhone 15", " iPhone-15 ", "Best phone", brandId, new List<Guid> { categoryId }),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _products.Verify(
            x => x.AddAsync(
                It.Is<Product>(p => p.Name == "iPhone 15"
                    && p.Slug == "iphone-15"
                    && p.BrandId == brandId
                    && p.Status == ProductStatus.Draft
                    && p.Categories.Count == 1
                    && p.Categories.Single().CategoryId == categoryId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCategoryIdsContainDuplicates_StoresDistinctCategoriesOnly()
    {
        var handler = CreateHandler();
        var categoryId = Guid.NewGuid();
        _products
            .Setup(x => x.SlugExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _categories
            .Setup(x => x.AllExistAsync(It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await handler.Handle(
            new CreateProductCommand("Phone", "phone", "Desc", CategoryIds: new List<Guid> { categoryId, categoryId }),
            CancellationToken.None);

        _products.Verify(
            x => x.AddAsync(It.Is<Product>(p => p.Categories.Count == 1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ThrowsConflictAndDoesNotSave()
    {
        var handler = CreateHandler();
        _products
            .Setup(x => x.SlugExistsAsync("iphone-15", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateProductCommand("iPhone 15", "iphone-15", "Best"), CancellationToken.None));

        Assert.Contains("'iphone-15'", exception.Message);
        _products.Verify(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBrandDoesNotExist_ThrowsNotFound()
    {
        var handler = CreateHandler();
        var brandId = Guid.NewGuid();
        _products
            .Setup(x => x.SlugExistsAsync("iphone-15", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _brands
            .Setup(x => x.ExistsAsync(brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CreateProductCommand("iPhone 15", "iphone-15", "Best", brandId), CancellationToken.None));

        Assert.Contains("Brand", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSomeCategoryDoesNotExist_ThrowsNotFound()
    {
        var handler = CreateHandler();
        var categoryId = Guid.NewGuid();
        _products
            .Setup(x => x.SlugExistsAsync("iphone-15", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _categories
            .Setup(x => x.AllExistAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CreateProductCommand(
                "iPhone 15", "iphone-15", "Best", CategoryIds: new List<Guid> { categoryId }),
                CancellationToken.None));

        Assert.Contains("Category", exception.Message);
        Assert.Contains(categoryId.ToString(), exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutBrandOrCategories_SkipsCrossAggregateChecks()
    {
        var handler = CreateHandler();
        _products
            .Setup(x => x.SlugExistsAsync("plain", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await handler.Handle(new CreateProductCommand("Plain", "plain", "No brand"), CancellationToken.None);

        _brands.Verify(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _categories.Verify(x => x.AllExistAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _products.Verify(
            x => x.AddAsync(It.Is<Product>(p => p.BrandId == null && p.Categories.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
