using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Products.Commands.UpdateProduct;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Products;

public sealed class UpdateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IBrandRepository> _brands = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateProductCommandHandler CreateHandler() =>
        new(_products.Object, _brands.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = CreateHandler();
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateProductCommand(productId, "iPhone", "iphone", "Best", null),
                CancellationToken.None));

        Assert.Contains("Product", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSlugOwnedByAnotherProduct_ThrowsConflict()
    {
        var handler = CreateHandler();
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Product.Create("iPhone", "iphone", "Best", null));
        _products
            .Setup(x => x.SlugExistsAsync("ipad", productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new UpdateProductCommand(productId, "iPad", "ipad", "Best", null),
                CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBrandDoesNotExist_ThrowsNotFound()
    {
        var handler = CreateHandler();
        var productId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Product.Create("iPhone", "iphone", "Best", null));
        _products
            .Setup(x => x.SlugExistsAsync("iphone", productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _brands
            .Setup(x => x.ExistsAsync(brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateProductCommand(productId, "iPhone", "iphone", "Best", brandId),
                CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSlugIsOwn_UpdatesDetailsAndSaves()
    {
        var handler = CreateHandler();
        var productId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var existing = Product.Create("Old Name", "old-slug", "Old description", null);
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _products
            .Setup(x => x.SlugExistsAsync("new-slug", productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _brands
            .Setup(x => x.ExistsAsync(brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await handler.Handle(
            new UpdateProductCommand(productId, "New Name", "New-Slug", "New description", brandId),
            CancellationToken.None);

        Assert.Equal("New Name", existing.Name);
        Assert.Equal("new-slug", existing.Slug);
        Assert.Equal("New description", existing.Description);
        Assert.Equal(brandId, existing.BrandId);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
