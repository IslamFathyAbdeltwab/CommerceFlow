using CommerceFlow.Application.Catalog.Categories.Commands.UpdateCategory;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Categories;

public sealed class UpdateCategoryCommandHandlerTests
{
    private static (UpdateCategoryCommandHandler Handler, Mock<ICategoryRepository> Categories, Mock<IUnitOfWork> UnitOfWork)
        CreateSut()
    {
        var categories = new Mock<ICategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new UpdateCategoryCommandHandler(categories.Object, unitOfWork.Object);
        return (handler, categories, unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotExist_ThrowsNotFound()
    {
        var (handler, categories, unitOfWork) = CreateSut();
        var categoryId = Guid.NewGuid();
        categories
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateCategoryCommand(categoryId, "Phones", "phones", null, 0, null),
                CancellationToken.None));

        Assert.Contains("Category", exception.Message);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSlugOwnedByAnotherCategory_ThrowsConflict()
    {
        var (handler, categories, unitOfWork) = CreateSut();
        var categoryId = Guid.NewGuid();
        categories
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Category.Create("Phones", "phones"));
        categories
            .Setup(x => x.SlugExistsAsync("laptops", categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new UpdateCategoryCommand(categoryId, "Laptops", "laptops", null, 0, null),
                CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenParentDoesNotExist_ThrowsNotFound()
    {
        var (handler, categories, _) = CreateSut();
        var categoryId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        categories
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Category.Create("Phones", "phones"));
        categories
            .Setup(x => x.SlugExistsAsync("phones", categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        categories
            .Setup(x => x.ExistsAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateCategoryCommand(categoryId, "Phones", "phones", null, 0, parentId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenParentIsOwnDescendant_ThrowsConflict()
    {
        var (handler, categories, unitOfWork) = CreateSut();
        var categoryId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        categories
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Category.Create("Electronics", "electronics"));
        categories
            .Setup(x => x.SlugExistsAsync("electronics", categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        categories
            .Setup(x => x.ExistsAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        categories
            .Setup(x => x.IsDescendantAsync(parentId, categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new UpdateCategoryCommand(categoryId, "Electronics", "electronics", null, 1, parentId),
                CancellationToken.None));

        Assert.Contains("descendant", exception.Message);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSlugIsOwn_UpdatesDetailsAndSaves()
    {
        var (handler, categories, unitOfWork) = CreateSut();
        var categoryId = Guid.NewGuid();
        var existing = Category.Create("Old", "old-slug");
        categories
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        categories
            .Setup(x => x.SlugExistsAsync("new-slug", categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await handler.Handle(
            new UpdateCategoryCommand(categoryId, "New Name", "New-Slug", "Updated", 5, null),
            CancellationToken.None);

        Assert.Equal("New Name", existing.Name);
        Assert.Equal("new-slug", existing.Slug);
        Assert.Equal("Updated", existing.Description);
        Assert.Equal(5, existing.DisplayOrder);
        Assert.Null(existing.ParentCategoryId);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
