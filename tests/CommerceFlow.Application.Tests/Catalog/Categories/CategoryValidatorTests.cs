using CommerceFlow.Application.Catalog.Categories.Commands.CreateCategory;
using CommerceFlow.Application.Catalog.Categories.Commands.UpdateCategory;
using CommerceFlow.Application.Catalog.Categories.Queries.GetCategories;
using CommerceFlow.Domain.Entities.Catalog;

namespace CommerceFlow.Application.Tests.Catalog.Categories;

public sealed class CategoryValidatorTests
{
    [Fact]
    public void CreateCategory_WithValidInput_IsValid()
    {
        var result = new CreateCategoryCommandValidator()
            .Validate(new CreateCategoryCommand("Electronics", "electronics", DisplayOrder: 1));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateCategory_WithEmptyName_IsInvalid()
    {
        var result = new CreateCategoryCommandValidator()
            .Validate(new CreateCategoryCommand("", "electronics"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCategoryCommand.Name));
    }

    [Theory]
    [InlineData("not a slug")]
    [InlineData("double--hyphen")]
    public void CreateCategory_WithMalformedSlug_IsInvalid(string slug)
    {
        var result = new CreateCategoryCommandValidator()
            .Validate(new CreateCategoryCommand("Electronics", slug));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCategoryCommand.Slug));
    }

    [Fact]
    public void CreateCategory_WithNegativeDisplayOrder_IsInvalid()
    {
        var result = new CreateCategoryCommandValidator()
            .Validate(new CreateCategoryCommand("Electronics", "electronics", DisplayOrder: -1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCategoryCommand.DisplayOrder));
    }

    [Fact]
    public void CreateCategory_WithEmptyParentId_IsInvalid()
    {
        var result = new CreateCategoryCommandValidator()
            .Validate(new CreateCategoryCommand("Electronics", "electronics", ParentCategoryId: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCategoryCommand.ParentCategoryId));
    }

    [Fact]
    public void CreateCategory_WithDescriptionOverMaxLength_IsInvalid()
    {
        var result = new CreateCategoryCommandValidator()
            .Validate(new CreateCategoryCommand(
                "Electronics",
                "electronics",
                new string('x', Category.DescriptionMaxLength + 1)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateCategory_WithValidInput_IsValid()
    {
        var result = new UpdateCategoryCommandValidator()
            .Validate(new UpdateCategoryCommand(Guid.NewGuid(), "Phones", "phones", null, 0, null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateCategory_WithEmptyCategoryId_IsInvalid()
    {
        var result = new UpdateCategoryCommandValidator()
            .Validate(new UpdateCategoryCommand(Guid.Empty, "Phones", "phones", null, 0, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCategoryCommand.CategoryId));
    }

    [Fact]
    public void UpdateCategory_WithItselfAsParent_IsInvalid()
    {
        var categoryId = Guid.NewGuid();
        var result = new UpdateCategoryCommandValidator()
            .Validate(new UpdateCategoryCommand(categoryId, "Phones", "phones", null, 0, categoryId));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "A category cannot be its own parent.");
    }

    [Fact]
    public void GetCategories_WithDefaultPaging_IsValid()
    {
        var result = new GetCategoriesQueryValidator().Validate(new GetCategoriesQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetCategories_WithOutOfRangePaging_IsInvalid()
    {
        var result = new GetCategoriesQueryValidator()
            .Validate(new GetCategoriesQuery(PageNumber: 1001, PageSize: 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetCategoriesQuery.PageNumber));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetCategoriesQuery.PageSize));
    }
}
