using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Products.Commands.ArchiveProduct;
using CommerceFlow.Application.Catalog.Products.Commands.FeatureProduct;
using CommerceFlow.Application.Catalog.Products.Commands.PublishProduct;
using CommerceFlow.Application.Catalog.Products.Commands.UnfeatureProduct;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Products;

public sealed class ProductLifecycleCommandHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static Product CreateDraftProduct(bool withVariant = true)
    {
        var product = Product.Create("iPhone", "iphone", "Best phone", null);
        if (withVariant)
            product.AddVariant("Base", "SKU-1", 100);
        return product;
    }

    private static Product CreatePublishedProduct()
    {
        var product = CreateDraftProduct();
        product.Publish();
        return product;
    }

    // ---------- Publish ----------

    [Fact]
    public async Task Publish_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new PublishProductCommandHandler(_products.Object, _unitOfWork.Object);
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new PublishProductCommand(productId), CancellationToken.None));

        Assert.Contains("Product", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Publish_WithoutActiveVariant_ThrowsDomainException()
    {
        var handler = new PublishProductCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateDraftProduct(withVariant: false);
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new PublishProductCommand(product.Id), CancellationToken.None));

        Assert.Equal(ProductStatus.Draft, product.Status);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Publish_WhenDraftWithActiveVariant_PublishesAndSaves()
    {
        var handler = new PublishProductCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateDraftProduct();
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        await handler.Handle(new PublishProductCommand(product.Id), CancellationToken.None);

        Assert.Equal(ProductStatus.Published, product.Status);
        Assert.NotNull(product.PublishedAtUtc);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_WhenAlreadyPublished_ThrowsDomainException()
    {
        var handler = new PublishProductCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreatePublishedProduct();
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new PublishProductCommand(product.Id), CancellationToken.None));
    }

    // ---------- Archive ----------

    [Fact]
    public async Task Archive_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new ArchiveProductCommandHandler(_products.Object, _unitOfWork.Object);
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ArchiveProductCommand(productId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Archive_WhenPublished_ClearsFeaturedFlagAndSaves()
    {
        var handler = new ArchiveProductCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreatePublishedProduct();
        product.Feature();
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        await handler.Handle(new ArchiveProductCommand(product.Id), CancellationToken.None);

        Assert.Equal(ProductStatus.Archived, product.Status);
        Assert.False(product.IsFeatured);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Archive_WhenAlreadyArchived_ThrowsDomainException()
    {
        var handler = new ArchiveProductCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreatePublishedProduct();
        product.Archive();
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ArchiveProductCommand(product.Id), CancellationToken.None));
    }

    // ---------- Feature / Unfeature ----------

    [Fact]
    public async Task Feature_WhenProductIsNotPublished_ThrowsDomainException()
    {
        var handler = new FeatureProductCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateDraftProduct();
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new FeatureProductCommand(product.Id), CancellationToken.None));

        Assert.False(product.IsFeatured);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Feature_WhenPublished_MarksFeaturedAndSaves()
    {
        var handler = new FeatureProductCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreatePublishedProduct();
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        await handler.Handle(new FeatureProductCommand(product.Id), CancellationToken.None);

        Assert.True(product.IsFeatured);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Feature_WhenAlreadyFeatured_IsIdempotent()
    {
        var handler = new FeatureProductCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreatePublishedProduct();
        product.Feature();
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        await handler.Handle(new FeatureProductCommand(product.Id), CancellationToken.None);

        Assert.True(product.IsFeatured);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unfeature_WhenFeatured_ClearsFlagAndSaves()
    {
        var handler = new UnfeatureProductCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreatePublishedProduct();
        product.Feature();
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        await handler.Handle(new UnfeatureProductCommand(product.Id), CancellationToken.None);

        Assert.False(product.IsFeatured);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unfeature_WhenNotFeatured_StillSavesWithoutError()
    {
        var handler = new UnfeatureProductCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreatePublishedProduct();
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        await handler.Handle(new UnfeatureProductCommand(product.Id), CancellationToken.None);

        Assert.False(product.IsFeatured);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unfeature_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new UnfeatureProductCommandHandler(_products.Object, _unitOfWork.Object);
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UnfeatureProductCommand(productId), CancellationToken.None));
    }
}
