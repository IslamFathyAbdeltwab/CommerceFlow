using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.ProductAttributes.Commands.CreateProductAttribute;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.ProductAttributes;

public sealed class CreateProductAttributeCommandHandlerTests
{
    private static (CreateProductAttributeCommandHandler Handler, Mock<IProductAttributeRepository> Attributes, Mock<IUnitOfWork> UnitOfWork)
        CreateSut()
    {
        var attributes = new Mock<IProductAttributeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateProductAttributeCommandHandler(attributes.Object, unitOfWork.Object);
        return (handler, attributes, unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenCodeIsFree_AddsAttributeAndSaves()
    {
        var (handler, attributes, unitOfWork) = CreateSut();
        attributes
            .Setup(x => x.CodeExistsAsync("color", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var id = await handler.Handle(
            new CreateProductAttributeCommand("Color", "color", ProductAttributeDataType.Text, true, true),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        attributes.Verify(
            x => x.AddAsync(
                It.Is<ProductAttribute>(a => a.Name == "Color"
                    && a.Code == "color"
                    && a.DataType == ProductAttributeDataType.Text
                    && a.IsVariantDefining
                    && a.IsFilterable),
                It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCodeHasMixedCase_ChecksAgainstNormalizedCode()
    {
        var (handler, attributes, _) = CreateSut();
        attributes
            .Setup(x => x.CodeExistsAsync("color", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await handler.Handle(
            new CreateProductAttributeCommand("Color", "  Color ", ProductAttributeDataType.Text, false, false),
            CancellationToken.None);

        attributes.Verify(
            x => x.CodeExistsAsync("color", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ThrowsConflictAndDoesNotSave()
    {
        var (handler, attributes, unitOfWork) = CreateSut();
        attributes
            .Setup(x => x.CodeExistsAsync("color", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new CreateProductAttributeCommand("Color", "color", ProductAttributeDataType.Text, true, true),
                CancellationToken.None));

        Assert.Contains("'color'", exception.Message);
        attributes.Verify(x => x.AddAsync(It.IsAny<ProductAttribute>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
