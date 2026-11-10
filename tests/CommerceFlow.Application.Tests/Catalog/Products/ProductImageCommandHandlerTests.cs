using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Products.Commands.AddProductImage;
using CommerceFlow.Application.Catalog.Products.Commands.RemoveProductImage;
using CommerceFlow.Application.Catalog.Products.Commands.SetProductPrimaryImage;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Exceptions;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Products;

public sealed class ProductImageCommandHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static Product CreateProduct()
    {
        var product = Product.Create("iPhone", "iphone", "Best phone", null);
        product.AddVariant("Base", "SKU-1", 100);
        return product;
    }

    private void SetupProduct(Product product) =>
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

    // ---------- Add ----------

    [Fact]
    public async Task AddImage_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new AddProductImageCommandHandler(_products.Object, _unitOfWork.Object);
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AddProductImageCommand(productId, "img/a.jpg", "Alt", 0), CancellationToken.None));

        Assert.Contains("Product", exception.Message);
    }

    [Fact]
    public async Task AddImage_FirstProductLevelImage_BecomesPrimaryAutomatically()
    {
        var handler = new AddProductImageCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProduct();
        SetupProduct(product);

        var imageId = await handler.Handle(
            new AddProductImageCommand(product.Id, "img/a.jpg", "Front", 0), CancellationToken.None);

        var image = Assert.Single(product.Images);
        Assert.Equal(imageId, image.Id);
        Assert.True(image.IsPrimary);
        Assert.Null(image.ProductVariantId);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddImage_ForUnknownVariant_ThrowsDomainException()
    {
        var handler = new AddProductImageCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProduct();
        SetupProduct(product);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new AddProductImageCommand(
                product.Id, "img/a.jpg", null, 0, VariantId: Guid.NewGuid()), CancellationToken.None));

        Assert.Empty(product.Images);
    }

    [Fact]
    public async Task AddImage_PrimaryFlagOnVariantImage_ThrowsDomainException()
    {
        var handler = new AddProductImageCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProduct();
        var variantId = product.Variants.Single().Id;
        SetupProduct(product);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new AddProductImageCommand(
                product.Id, "img/a.jpg", null, 0, IsPrimary: true, VariantId: variantId), CancellationToken.None));
    }

    // ---------- Remove ----------

    [Fact]
    public async Task RemoveImage_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new RemoveProductImageCommandHandler(_products.Object, _unitOfWork.Object);
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RemoveProductImageCommand(productId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task RemoveImage_WhenPrimaryRemoved_PromotesNextProductLevelImage()
    {
        var handler = new RemoveProductImageCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProduct();
        product.AddImage("img/a.jpg", "First", 0); // auto primary
        product.AddImage("img/b.jpg", "Second", 1);
        var primary = product.Images.Single(i => i.IsPrimary);
        SetupProduct(product);

        await handler.Handle(new RemoveProductImageCommand(product.Id, primary.Id), CancellationToken.None);

        var remaining = Assert.Single(product.Images);
        Assert.Equal("img/b.jpg", remaining.StorageKey);
        Assert.True(remaining.IsPrimary);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveImage_WhenImageDoesNotExist_ThrowsDomainException()
    {
        var handler = new RemoveProductImageCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProduct();
        SetupProduct(product);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new RemoveProductImageCommand(product.Id, Guid.NewGuid()), CancellationToken.None));
    }

    // ---------- Set primary ----------

    [Fact]
    public async Task SetPrimaryImage_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new SetProductPrimaryImageCommandHandler(_products.Object, _unitOfWork.Object);
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new SetProductPrimaryImageCommand(productId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task SetPrimaryImage_MovesPrimaryFlagToChosenImage()
    {
        var handler = new SetProductPrimaryImageCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProduct();
        product.AddImage("img/a.jpg", "First", 0); // auto primary
        product.AddImage("img/b.jpg", "Second", 1);
        var second = product.Images.Single(i => i.StorageKey == "img/b.jpg");
        SetupProduct(product);

        await handler.Handle(new SetProductPrimaryImageCommand(product.Id, second.Id), CancellationToken.None);

        Assert.False(product.Images.Single(i => i.StorageKey == "img/a.jpg").IsPrimary);
        Assert.True(second.IsPrimary);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetPrimaryImage_WhenImageBelongsToVariant_ThrowsDomainException()
    {
        var handler = new SetProductPrimaryImageCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProduct();
        var variantId = product.Variants.Single().Id;
        product.AddImage("img/a.jpg", "First", 0); // product-level, auto primary
        var variantImageId = product.AddImage("img/variant.jpg", "Variant", 1, variantId: variantId);
        SetupProduct(product);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new SetProductPrimaryImageCommand(product.Id, variantImageId), CancellationToken.None));
    }
}
