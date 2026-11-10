using CommerceFlow.Application.Catalog.Categories.Queries.GetCategories;
using CommerceFlow.Application.Catalog.Categories.Queries.GetCategoryById;
using CommerceFlow.Application.Catalog.Categories.Queries.GetCategoryTree;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Categories;

public sealed class CategoryQueryHandlerTests
{
    private readonly Mock<ICategoryRepository> _categories = new();

    [Fact]
    public async Task GetCategoryById_WhenCategoryDoesNotExist_ReturnsNull()
    {
        var handler = new GetCategoryByIdQueryHandler(_categories.Object);
        _categories
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var result = await handler.Handle(new GetCategoryByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCategoryById_WhenCategoryExists_MapsAllFields()
    {
        var handler = new GetCategoryByIdQueryHandler(_categories.Object);
        var parentId = Guid.NewGuid();
        var category = Category.Create("Phones", "phones", "Mobile", 3, parentId);
        _categories
            .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var result = await handler.Handle(new GetCategoryByIdQuery(category.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(category.Id, result.Id);
        Assert.Equal("Phones", result.Name);
        Assert.Equal("phones", result.Slug);
        Assert.Equal("Mobile", result.Description);
        Assert.Equal(parentId, result.ParentCategoryId);
        Assert.Equal(3, result.DisplayOrder);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetCategoryTree_BuildsHierarchyWithRootsOrderedByDisplayOrder()
    {
        var handler = new GetCategoryTreeQueryHandler(_categories.Object);
        var electronics = Category.Create("Electronics", "electronics");
        var phones = Category.Create("Phones", "phones", parentCategoryId: electronics.Id);
        var books = Category.Create("Books", "books", displayOrder: 1);
        // Parent not part of the result set, so this one must surface as a root.
        var orphan = Category.Create("Orphan", "orphan", parentCategoryId: Guid.NewGuid());
        _categories
            .Setup(x => x.GetAllAsync(null, true, 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { phones, electronics, orphan, books });

        var result = await handler.Handle(new GetCategoryTreeQuery(), CancellationToken.None);

        Assert.Equal(3, result.Count);
        // Roots ordered by DisplayOrder then Name: Electronics (0), Orphan (0), Books (1).
        Assert.Equal(new[] { "Electronics", "Orphan", "Books" }, result.Select(c => c.Name));

        var electronicsNode = result.Single(c => c.Name == "Electronics");
        var child = Assert.Single(electronicsNode.Children);
        Assert.Equal("Phones", child.Name);
        Assert.Empty(result.Single(c => c.Name == "Orphan").Children);
        Assert.Empty(result.Single(c => c.Name == "Books").Children);
    }

    [Fact]
    public async Task GetCategoryTree_WhenOnlyActive_Disabled_PassesNullActiveFilter()
    {
        var handler = new GetCategoryTreeQueryHandler(_categories.Object);
        _categories
            .Setup(x => x.GetAllAsync(null, null, 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        var result = await handler.Handle(new GetCategoryTreeQuery(OnlyActive: false), CancellationToken.None);

        Assert.Empty(result);
        _categories.Verify(
            x => x.GetAllAsync(null, null, 1, int.MaxValue, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCategories_ReturnsPagedResultWithMappedItems()
    {
        var handler = new GetCategoriesQueryHandler(_categories.Object);
        var root = Category.Create("Electronics", "electronics");
        var child = Category.Create("Phones", "phones", parentCategoryId: root.Id, displayOrder: 2);
        _categories
            .Setup(x => x.CountAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);
        _categories
            .Setup(x => x.GetAllAsync(null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { root, child });

        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        Assert.Equal(15, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(root.Id, result.Items[0].Id);
        Assert.Equal(root.Id, result.Items[1].ParentCategoryId);
        Assert.Equal(2, result.Items[1].DisplayOrder);
    }
}
