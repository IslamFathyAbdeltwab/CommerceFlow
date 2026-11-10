using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Products.Commands.RemoveVariantAttributeValue;
using CommerceFlow.Application.Catalog.Products.Commands.SetVariantAttributeValue;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;
using FluentValidation;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Products;

public sealed class ProductVariantAttributeValueCommandHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IProductAttributeRepository> _attributes = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private SetVariantAttributeValueCommandHandler CreateSetHandler() =>
        new(_products.Object, _attributes.Object, _unitOfWork.Object);

    private RemoveVariantAttributeValueCommandHandler CreateRemoveHandler() =>
        new(_products.Object, _unitOfWork.Object);

    private static (Product Product, ProductVariant Variant) CreateProductWithVariant()
    {
        var product = Product.Create("iPhone", "iphone", "Best phone", null);
        product.AddVariant("Base", "SKU-1", 100);
        return (product, product.Variants.Single());
    }

    private void SetupProduct(Product product) =>
        _products
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

    // ---------- Set ----------

    [Fact]
    public async Task Set_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = CreateSetHandler();
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new SetVariantAttributeValueCommand(
                productId, Guid.NewGuid(), Guid.NewGuid(), "Blue"), CancellationToken.None));

        Assert.Contains("Product", exception.Message);
    }

    [Fact]
    public async Task Set_WhenVariantDoesNotBelongToProduct_ThrowsNotFound()
    {
        var handler = CreateSetHandler();
        var (product, _) = CreateProductWithVariant();
        SetupProduct(product);
        var variantId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new SetVariantAttributeValueCommand(
                product.Id, variantId, Guid.NewGuid(), "Blue"), CancellationToken.None));

        Assert.Contains("ProductVariant", exception.Message);
    }

    [Fact]
    public async Task Set_WhenAttributeDoesNotExist_ThrowsNotFound()
    {
        var handler = CreateSetHandler();
        var (product, variant) = CreateProductWithVariant();
        SetupProduct(product);
        var attributeId = Guid.NewGuid();
        _attributes
            .Setup(x => x.GetByIdAsync(attributeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new SetVariantAttributeValueCommand(
                product.Id, variant.Id, attributeId, "Blue"), CancellationToken.None));

        Assert.Contains("ProductAttribute", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Set_WhenValueDoesNotMatchNumberDataType_ThrowsValidationException()
    {
        var handler = CreateSetHandler();
        var (product, variant) = CreateProductWithVariant();
        SetupProduct(product);
        var attribute = ProductAttribute.Create("Weight", "weight", ProductAttributeDataType.Number, false, true);
        _attributes
            .Setup(x => x.GetByIdAsync(attribute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new SetVariantAttributeValueCommand(
                product.Id, variant.Id, attribute.Id, "not-a-number"), CancellationToken.None));

        Assert.Contains("Value must be a valid number.", exception.Message);
        Assert.Empty(variant.AttributeValues);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Set_WhenValueDoesNotMatchBooleanDataType_ThrowsValidationException()
    {
        var handler = CreateSetHandler();
        var (product, variant) = CreateProductWithVariant();
        SetupProduct(product);
        var attribute = ProductAttribute.Create("Fragile", "fragile", ProductAttributeDataType.Boolean, false, false);
        _attributes
            .Setup(x => x.GetByIdAsync(attribute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new SetVariantAttributeValueCommand(
                product.Id, variant.Id, attribute.Id, "maybe"), CancellationToken.None));
    }

    [Fact]
    public async Task Set_WhenValueMatchesDataType_StoresValueAndSaves()
    {
        var handler = CreateSetHandler();
        var (product, variant) = CreateProductWithVariant();
        SetupProduct(product);
        var attribute = ProductAttribute.Create("Weight", "weight", ProductAttributeDataType.Number, false, true);
        _attributes
            .Setup(x => x.GetByIdAsync(attribute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        await handler.Handle(new SetVariantAttributeValueCommand(
            product.Id, variant.Id, attribute.Id, "42.5"), CancellationToken.None);

        var value = Assert.Single(variant.AttributeValues);
        Assert.Equal(attribute.Id, value.ProductAttributeId);
        Assert.Equal("42.5", value.Value);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Set_WhenValueAlreadyExists_ReplacesExistingValue()
    {
        var handler = CreateSetHandler();
        var (product, variant) = CreateProductWithVariant();
        SetupProduct(product);
        var attribute = ProductAttribute.Create("Color", "color", ProductAttributeDataType.Text, true, true);
        _attributes
            .Setup(x => x.GetByIdAsync(attribute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        await handler.Handle(new SetVariantAttributeValueCommand(
            product.Id, variant.Id, attribute.Id, "Blue"), CancellationToken.None);
        await handler.Handle(new SetVariantAttributeValueCommand(
            product.Id, variant.Id, attribute.Id, "Red"), CancellationToken.None);

        var value = Assert.Single(variant.AttributeValues);
        Assert.Equal("Red", value.Value);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // ---------- Remove ----------

    [Fact]
    public async Task Remove_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = CreateRemoveHandler();
        var productId = Guid.NewGuid();
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RemoveVariantAttributeValueCommand(
                productId, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Remove_WhenValueExists_RemovesItAndSaves()
    {
        var handler = CreateRemoveHandler();
        var (product, variant) = CreateProductWithVariant();
        var attributeId = Guid.NewGuid();
        product.SetVariantAttributeValue(variant.Id, attributeId, "Blue");
        SetupProduct(product);

        await handler.Handle(new RemoveVariantAttributeValueCommand(
            product.Id, variant.Id, attributeId), CancellationToken.None);

        Assert.Empty(variant.AttributeValues);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remove_WhenValueDoesNotExist_ThrowsDomainException()
    {
        var handler = CreateRemoveHandler();
        var (product, variant) = CreateProductWithVariant();
        SetupProduct(product);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new RemoveVariantAttributeValueCommand(
                product.Id, variant.Id, Guid.NewGuid()), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
