using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.ProductAttributes.Commands.UpdateProductAttribute;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.ProductAttributes;

public sealed class UpdateProductAttributeCommandHandlerTests
{
    private static (UpdateProductAttributeCommandHandler Handler, Mock<IProductAttributeRepository> Attributes, Mock<IUnitOfWork> UnitOfWork)
        CreateSut()
    {
        var attributes = new Mock<IProductAttributeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new UpdateProductAttributeCommandHandler(attributes.Object, unitOfWork.Object);
        return (handler, attributes, unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenAttributeDoesNotExist_ThrowsNotFound()
    {
        var (handler, attributes, unitOfWork) = CreateSut();
        var attributeId = Guid.NewGuid();
        attributes
            .Setup(x => x.GetByIdAsync(attributeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateProductAttributeCommand(
                attributeId, "Color", "color", ProductAttributeDataType.Text, true, true),
                CancellationToken.None));

        Assert.Contains("ProductAttribute", exception.Message);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCodeOwnedByAnotherAttribute_ThrowsConflict()
    {
        var (handler, attributes, unitOfWork) = CreateSut();
        var attributeId = Guid.NewGuid();
        attributes
            .Setup(x => x.GetByIdAsync(attributeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductAttribute.Create("Size", "size", ProductAttributeDataType.Text, true, true));
        attributes
            .Setup(x => x.CodeExistsAsync("color", attributeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new UpdateProductAttributeCommand(
                attributeId, "Color", "Color", ProductAttributeDataType.Text, true, true),
                CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCodeIsOwn_UpdatesDetailsAndSaves()
    {
        var (handler, attributes, unitOfWork) = CreateSut();
        var attributeId = Guid.NewGuid();
        var existing = ProductAttribute.Create("Old", "old-code", ProductAttributeDataType.Text, false, false);
        attributes
            .Setup(x => x.GetByIdAsync(attributeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        attributes
            .Setup(x => x.CodeExistsAsync("new-code", attributeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await handler.Handle(new UpdateProductAttributeCommand(
            attributeId, "New Name", "New-Code", ProductAttributeDataType.Number, true, true),
            CancellationToken.None);

        Assert.Equal("New Name", existing.Name);
        Assert.Equal("new-code", existing.Code);
        Assert.Equal(ProductAttributeDataType.Number, existing.DataType);
        Assert.True(existing.IsVariantDefining);
        Assert.True(existing.IsFilterable);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
