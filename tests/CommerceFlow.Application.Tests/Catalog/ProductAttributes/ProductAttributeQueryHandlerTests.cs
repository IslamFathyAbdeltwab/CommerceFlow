using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.ProductAttributes.Queries.GetProductAttributeById;
using CommerceFlow.Application.Catalog.ProductAttributes.Queries.GetProductAttributes;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.ProductAttributes;

public sealed class ProductAttributeQueryHandlerTests
{
    private readonly Mock<IProductAttributeRepository> _attributes = new();

    [Fact]
    public async Task GetProductAttributeById_WhenAttributeDoesNotExist_ReturnsNull()
    {
        var handler = new GetProductAttributeByIdQueryHandler(_attributes.Object);
        _attributes
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        var result = await handler.Handle(new GetProductAttributeByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductAttributeById_WhenAttributeExists_MapsAllFields()
    {
        var handler = new GetProductAttributeByIdQueryHandler(_attributes.Object);
        var attribute = ProductAttribute.Create("Color", "color", ProductAttributeDataType.Text, true, true);
        _attributes
            .Setup(x => x.GetByIdAsync(attribute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var result = await handler.Handle(new GetProductAttributeByIdQuery(attribute.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(attribute.Id, result.Id);
        Assert.Equal("Color", result.Name);
        Assert.Equal("color", result.Code);
        Assert.Equal(ProductAttributeDataType.Text, result.DataType);
        Assert.True(result.IsVariantDefining);
        Assert.True(result.IsFilterable);
        Assert.Equal(attribute.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(attribute.UpdatedAtUtc, result.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetProductAttributes_ReturnsPagedResultWithMappedItems()
    {
        var handler = new GetProductAttributesQueryHandler(_attributes.Object);
        var color = ProductAttribute.Create("Color", "color", ProductAttributeDataType.Text, true, true);
        var weight = ProductAttribute.Create("Weight", "weight", ProductAttributeDataType.Number, false, true);
        _attributes
            .Setup(x => x.CountAsync("e", true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);
        _attributes
            .Setup(x => x.GetAllAsync("e", true, null, 3, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { color, weight });

        var result = await handler.Handle(
            new GetProductAttributesQuery(" e ", IsVariantDefining: true, PageNumber: 3, PageSize: 50),
            CancellationToken.None);

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.PageNumber);
        Assert.Equal(50, result.PageSize);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("color", result.Items[0].Code);
        Assert.Equal(ProductAttributeDataType.Number, result.Items[1].DataType);
        Assert.False(result.Items[1].IsVariantDefining);
    }
}
