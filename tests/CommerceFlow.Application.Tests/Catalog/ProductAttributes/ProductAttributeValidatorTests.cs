using CommerceFlow.Application.Catalog.ProductAttributes.Commands.CreateProductAttribute;
using CommerceFlow.Application.Catalog.ProductAttributes.Commands.UpdateProductAttribute;
using CommerceFlow.Application.Catalog.ProductAttributes.Queries.GetProductAttributes;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Tests.Catalog.ProductAttributes;

public sealed class ProductAttributeValidatorTests
{
    private static CreateProductAttributeCommand ValidCreate() =>
        new("Color", "color", ProductAttributeDataType.Text, true, true);

    [Fact]
    public void CreateProductAttribute_WithValidInput_IsValid()
    {
        var result = new CreateProductAttributeCommandValidator().Validate(ValidCreate());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateProductAttribute_WithEmptyName_IsInvalid()
    {
        var result = new CreateProductAttributeCommandValidator()
            .Validate(ValidCreate() with { Name = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductAttributeCommand.Name));
    }

    [Theory]
    [InlineData("color code")]
    [InlineData("color_code")]
    [InlineData("color/code")]
    public void CreateProductAttribute_WithMalformedCode_IsInvalid(string code)
    {
        var result = new CreateProductAttributeCommandValidator()
            .Validate(ValidCreate() with { Code = code });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductAttributeCommand.Code));
    }

    [Fact]
    public void CreateProductAttribute_WithCodeOverMaxLength_IsInvalid()
    {
        var result = new CreateProductAttributeCommandValidator()
            .Validate(ValidCreate() with { Code = new string('x', ProductAttribute.CodeMaxLength + 1) });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateProductAttribute_WithUndefinedDataType_IsInvalid()
    {
        var result = new CreateProductAttributeCommandValidator()
            .Validate(ValidCreate() with { DataType = (ProductAttributeDataType)999 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductAttributeCommand.DataType));
    }

    [Fact]
    public void UpdateProductAttribute_WithEmptyAttributeId_IsInvalid()
    {
        var result = new UpdateProductAttributeCommandValidator()
            .Validate(new UpdateProductAttributeCommand(
                Guid.Empty, "Color", "color", ProductAttributeDataType.Text, true, true));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProductAttributeCommand.AttributeId));
    }

    [Fact]
    public void UpdateProductAttribute_WithValidInput_IsValid()
    {
        var result = new UpdateProductAttributeCommandValidator()
            .Validate(new UpdateProductAttributeCommand(
                Guid.NewGuid(), "Color", "color", ProductAttributeDataType.Text, true, true));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetProductAttributes_WithDefaultPaging_IsValid()
    {
        var result = new GetProductAttributesQueryValidator().Validate(new GetProductAttributesQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetProductAttributes_WithOutOfRangePaging_IsInvalid()
    {
        var result = new GetProductAttributesQueryValidator()
            .Validate(new GetProductAttributesQuery(PageNumber: 0, PageSize: 101));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetProductAttributesQuery.PageNumber));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetProductAttributesQuery.PageSize));
    }
}
