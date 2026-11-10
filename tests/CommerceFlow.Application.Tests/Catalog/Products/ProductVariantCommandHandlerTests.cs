using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Products.Commands.ActivateVariant;
using CommerceFlow.Application.Catalog.Products.Commands.AddProductVariant;
using CommerceFlow.Application.Catalog.Products.Commands.ChangeVariantPrice;
using CommerceFlow.Application.Catalog.Products.Commands.DeactivateVariant;
using CommerceFlow.Application.Catalog.Products.Commands.RemoveVariant;
using CommerceFlow.Application.Catalog.Products.Commands.UpdateProductVariant;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Products;

public sealed class ProductVariantCommandHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static Product CreateProductWithVariant(
        out ProductVariant variant,
        ProductStatus status = ProductStatus.Draft)
    {
        var product = Product.Create("iPhone", "iphone", "Best phone", null);
        product.AddVariant("Base", "SKU-1", 100);
        variant = product.Variants.Single();

        if (status == ProductStatus.Published)
            product.Publish();

        return product;
    }

    private void SetupProduct(Product product) =>
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

    // ---------- Add ----------

    [Fact]
    public async Task AddVariant_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new AddProductVariantCommandHandler(_products.Object, _unitOfWork.Object);
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AddProductVariantCommand(productId, "Base", "SKU-2", 50), CancellationToken.None));

        Assert.Contains("Product", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddVariant_WhenSkuExistsGlobally_ThrowsConflict()
    {
        var handler = new AddProductVariantCommandHandler(_products.Object, _unitOfWork.Object);
        var product = Product.Create("iPhone", "iphone", "Best phone", null);
        SetupProduct(product);
        _products
            .Setup(x => x.SkuExistsAsync("SKU-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new AddProductVariantCommand(product.Id, "Base", "sku-1", 50), CancellationToken.None));

        Assert.Contains("'SKU-1'", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddVariant_WhenSkuIsFree_AddsVariantWithNormalizedSku()
    {
        var handler = new AddProductVariantCommandHandler(_products.Object, _unitOfWork.Object);
        var product = Product.Create("iPhone", "iphone", "Best phone", null);
        SetupProduct(product);
        _products
            .Setup(x => x.SkuExistsAsync("SKU-2", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var variantId = await handler.Handle(
            new AddProductVariantCommand(product.Id, "Pro", " sku-2 ", 99.99m, 129.99m),
            CancellationToken.None);

        var variant = Assert.Single(product.Variants);
        Assert.Equal(variantId, variant.Id);
        Assert.Equal("SKU-2", variant.Sku);
        Assert.Equal(99.99m, variant.Price);
        Assert.Equal(129.99m, variant.CompareAtPrice);
        Assert.True(variant.IsActive);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Update ----------

    [Fact]
    public async Task UpdateVariant_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new UpdateProductVariantCommandHandler(_products.Object, _unitOfWork.Object);
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateProductVariantCommand(productId, Guid.NewGuid(), "Base", "SKU-1", 100),
                CancellationToken.None));
    }

    [Fact]
    public async Task UpdateVariant_WhenSkuOwnedByAnotherVariant_ThrowsConflict()
    {
        var handler = new UpdateProductVariantCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProductWithVariant(out var variant);
        SetupProduct(product);
        _products
            .Setup(x => x.SkuExistsAsync("SKU-2", variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new UpdateProductVariantCommand(
                product.Id, variant.Id, "Base", "sku-2", 100), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateVariant_WhenSkuIsOwn_UpdatesVariantDetailsAndSaves()
    {
        var handler = new UpdateProductVariantCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProductWithVariant(out var variant);
        SetupProduct(product);
        _products
            .Setup(x => x.SkuExistsAsync("SKU-1", variant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await handler.Handle(new UpdateProductVariantCommand(
            product.Id, variant.Id, "Base v2", "sku-1", 149.99m, "123456789", 199.99m, 300),
            CancellationToken.None);

        Assert.Equal("Base v2", variant.Name);
        Assert.Equal("SKU-1", variant.Sku);
        Assert.Equal(149.99m, variant.Price);
        Assert.Equal("123456789", variant.Barcode);
        Assert.Equal(199.99m, variant.CompareAtPrice);
        Assert.Equal(300, variant.WeightGrams);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Activate / Deactivate ----------

    [Fact]
    public async Task ActivateVariant_WhenVariantIsInactive_ActivatesAndSaves()
    {
        var handler = new ActivateVariantCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProductWithVariant(out var variant);
        product.DeactivateVariant(variant.Id);
        SetupProduct(product);

        await handler.Handle(new ActivateVariantCommand(product.Id, variant.Id), CancellationToken.None);

        Assert.True(variant.IsActive);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateVariant_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new DeactivateVariantCommandHandler(_products.Object, _unitOfWork.Object);
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeactivateVariantCommand(productId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task DeactivateVariant_WhenActive_DeactivatesAndSaves()
    {
        var handler = new DeactivateVariantCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProductWithVariant(out var variant);
        SetupProduct(product);

        await handler.Handle(new DeactivateVariantCommand(product.Id, variant.Id), CancellationToken.None);

        Assert.False(variant.IsActive);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateVariant_WhenItIsTheLastActiveVariantOfPublishedProduct_ThrowsDomainException()
    {
        var handler = new DeactivateVariantCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProductWithVariant(out var variant, ProductStatus.Published);
        SetupProduct(product);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new DeactivateVariantCommand(product.Id, variant.Id), CancellationToken.None));

        Assert.True(variant.IsActive);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Remove ----------

    [Fact]
    public async Task RemoveVariant_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new RemoveVariantCommandHandler(_products.Object, _unitOfWork.Object);
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RemoveVariantCommand(productId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task RemoveVariant_WhenAllowed_RemovesVariantAndItsImages()
    {
        var handler = new RemoveVariantCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProductWithVariant(out var variant);
        product.AddImage("variants/base.jpg", "Base", 0, variantId: variant.Id);
        SetupProduct(product);

        await handler.Handle(new RemoveVariantCommand(product.Id, variant.Id), CancellationToken.None);

        Assert.Empty(product.Variants);
        Assert.Empty(product.Images);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveVariant_WhenItIsTheLastActiveVariantOfPublishedProduct_ThrowsDomainException()
    {
        var handler = new RemoveVariantCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProductWithVariant(out var variant, ProductStatus.Published);
        SetupProduct(product);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new RemoveVariantCommand(product.Id, variant.Id), CancellationToken.None));

        Assert.Single(product.Variants);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Change price ----------

    [Fact]
    public async Task ChangePrice_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new ChangeVariantPriceCommandHandler(_products.Object, _unitOfWork.Object);
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ChangeVariantPriceCommand(productId, Guid.NewGuid(), 50), CancellationToken.None));
    }

    [Fact]
    public async Task ChangePrice_WhenValid_UpdatesPriceAndSaves()
    {
        var handler = new ChangeVariantPriceCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProductWithVariant(out var variant);
        SetupProduct(product);

        await handler.Handle(
            new ChangeVariantPriceCommand(product.Id, variant.Id, 80m, 120m),
            CancellationToken.None);

        Assert.Equal(80m, variant.Price);
        Assert.Equal(120m, variant.CompareAtPrice);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePrice_WhenCompareAtPriceNotGreaterThanPrice_ThrowsDomainException()
    {
        var handler = new ChangeVariantPriceCommandHandler(_products.Object, _unitOfWork.Object);
        var product = CreateProductWithVariant(out var variant);
        SetupProduct(product);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ChangeVariantPriceCommand(product.Id, variant.Id, 100m, 90m), CancellationToken.None));

        Assert.Equal(100m, variant.Price);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
