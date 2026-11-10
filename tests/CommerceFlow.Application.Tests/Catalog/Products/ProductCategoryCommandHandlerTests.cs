using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Products.Commands.AddProductCategory;
using CommerceFlow.Application.Catalog.Products.Commands.RemoveProductCategory;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Exceptions;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Products;

public sealed class ProductCategoryCommandHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static Product CreateProduct() =>
        Product.Create("iPhone", "iphone", "Best phone", null);

    private void SetupProduct(Product product) =>
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

    // ---------- Add ----------

    [Fact]
    public async Task AddCategory_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new AddProductCategoryCommandHandler(_products.Object, _categories.Object, _unitOfWork.Object);
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AddProductCategoryCommand(productId, Guid.NewGuid()), CancellationToken.None));

        Assert.Contains("Product", exception.Message);
    }

    [Fact]
    public async Task AddCategory_WhenCategoryDoesNotExist_ThrowsNotFound()
    {
        var handler = new AddProductCategoryCommandHandler(_products.Object, _categories.Object, _unitOfWork.Object);
        var product = CreateProduct();
        SetupProduct(product);
        var categoryId = Guid.NewGuid();
        _categories
            .Setup(x => x.ExistsAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AddProductCategoryCommand(product.Id, categoryId), CancellationToken.None));

        Assert.Contains("Category", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddCategory_WhenCategoryExists_LinksAndSaves()
    {
        var handler = new AddProductCategoryCommandHandler(_products.Object, _categories.Object, _unitOfWork.Object);
        var product = CreateProduct();
        SetupProduct(product);
        var categoryId = Guid.NewGuid();
        _categories
            .Setup(x => x.ExistsAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await handler.Handle(new AddProductCategoryCommand(product.Id, categoryId), CancellationToken.None);

        var link = Assert.Single(product.Categories);
        Assert.Equal(categoryId, link.CategoryId);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddCategory_WhenAlreadyLinked_ThrowsDomainException()
    {
        var handler = new AddProductCategoryCommandHandler(_products.Object, _categories.Object, _unitOfWork.Object);
        var product = CreateProduct();
        var categoryId = Guid.NewGuid();
        product.AddCategory(categoryId);
        SetupProduct(product);
        _categories
            .Setup(x => x.ExistsAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new AddProductCategoryCommand(product.Id, categoryId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Remove ----------

    [Fact]
    public async Task RemoveCategory_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new RemoveProductCategoryCommandHandler(_products.Object, _unitOfWork.Object);
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RemoveProductCategoryCommand(productId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task RemoveCategory_WhenLinked_UnlinksAndSaves()
    {
        var handler = new RemoveProductCategoryCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProduct();
        var categoryId = Guid.NewGuid();
        product.AddCategory(categoryId);
        SetupProduct(product);

        await handler.Handle(new RemoveProductCategoryCommand(product.Id, categoryId), CancellationToken.None);

        Assert.Empty(product.Categories);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveCategory_WhenNotLinked_ThrowsDomainException()
    {
        var handler = new RemoveProductCategoryCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProduct();
        SetupProduct(product);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new RemoveProductCategoryCommand(product.Id, Guid.NewGuid()), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
