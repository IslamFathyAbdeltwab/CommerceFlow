using CommerceFlow.Application.Catalog.Brands.Commands.CreateBrand;
using CommerceFlow.Application.Catalog.Brands.Commands.UpdateBrand;
using CommerceFlow.Application.Catalog.Brands.Queries.GetBrands;
using CommerceFlow.Domain.Entities.Catalog;

namespace CommerceFlow.Application.Tests.Catalog.Brands;

public sealed class BrandValidatorTests
{
    [Fact]
    public void CreateBrand_WithValidInput_IsValid()
    {
        var result = new CreateBrandCommandValidator()
            .Validate(new CreateBrandCommand("Apple", "apple-2", "Smartphones", "https://logo.png"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateBrand_WithEmptyName_IsInvalid()
    {
        var result = new CreateBrandCommandValidator().Validate(new CreateBrandCommand(" ", "apple"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBrandCommand.Name));
    }

    [Theory]
    [InlineData("not a slug")]
    [InlineData("double--hyphen")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    public void CreateBrand_WithMalformedSlug_IsInvalid(string slug)
    {
        var result = new CreateBrandCommandValidator().Validate(new CreateBrandCommand("Apple", slug));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBrandCommand.Slug));
    }

    [Fact]
    public void CreateBrand_WithNameOverMaxLength_IsInvalid()
    {
        var result = new CreateBrandCommandValidator()
            .Validate(new CreateBrandCommand(new string('x', Brand.NameMaxLength + 1), "apple"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateBrand_WithDescriptionOverMaxLength_IsInvalid()
    {
        var result = new CreateBrandCommandValidator()
            .Validate(new CreateBrandCommand("Apple", "apple", new string('x', Brand.DescriptionMaxLength + 1)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateBrand_WithLogoUrlOverMaxLength_IsInvalid()
    {
        var result = new CreateBrandCommandValidator()
            .Validate(new CreateBrandCommand("Apple", "apple", LogoUrl: new string('x', Brand.LogoUrlMaxLength + 1)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateBrand_WithEmptyBrandId_IsInvalid()
    {
        var result = new UpdateBrandCommandValidator()
            .Validate(new UpdateBrandCommand(Guid.Empty, "Apple", "apple"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBrandCommand.BrandId));
    }

    [Fact]
    public void UpdateBrand_WithValidInput_IsValid()
    {
        var result = new UpdateBrandCommandValidator()
            .Validate(new UpdateBrandCommand(Guid.NewGuid(), "Apple", "apple"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetBrands_WithDefaultPaging_IsValid()
    {
        var result = new GetBrandsQueryValidator().Validate(new GetBrandsQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetBrands_WithOutOfRangePaging_IsInvalid()
    {
        var result = new GetBrandsQueryValidator()
            .Validate(new GetBrandsQuery(PageNumber: 0, PageSize: 101));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetBrandsQuery.PageNumber));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetBrandsQuery.PageSize));
    }

    [Fact]
    public void GetBrands_WithTooLongSearchTerm_IsInvalid()
    {
        var result = new GetBrandsQueryValidator()
            .Validate(new GetBrandsQuery(SearchTerm: new string('x', 201)));

        Assert.False(result.IsValid);
    }
}
