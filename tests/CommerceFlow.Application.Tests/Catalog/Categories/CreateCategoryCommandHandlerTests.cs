using CommerceFlow.Application.Catalog.Categories.Commands.CreateCategory;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Categories;

public sealed class CreateCategoryCommandHandlerTests
{
    private static (CreateCategoryCommandHandler Handler, Mock<ICategoryRepository> Categories, Mock<IUnitOfWork> UnitOfWork)
        CreateSut()
    {
        var categories = new Mock<ICategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateCategoryCommandHandler(categories.Object, unitOfWork.Object);
        return (handler, categories, unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenSlugIsFree_AddsRootCategoryAndSaves()
    {
        var (handler, categories, unitOfWork) = CreateSut();
        categories
            .Setup(x => x.SlugExistsAsync("electronics", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var id = await handler.Handle(
            new CreateCategoryCommand("Electronics", "Electronics", "Devices", DisplayOrder: 2),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        categories.Verify(
            x => x.AddAsync(
                It.Is<Category>(c => c.Name == "Electronics"
                    && c.Slug == "electronics"
                    && c.DisplayOrder == 2
                    && c.ParentCategoryId == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ThrowsConflictAndDoesNotSave()
    {
        var (handler, categories, unitOfWork) = CreateSut();
        categories
            .Setup(x => x.SlugExistsAsync("electronics", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateCategoryCommand("Electronics", "electronics"), CancellationToken.None));

        Assert.Contains("'electronics'", exception.Message);
        categories.Verify(x => x.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenParentDoesNotExist_ThrowsNotFound()
    {
        var (handler, categories, unitOfWork) = CreateSut();
        var parentId = Guid.NewGuid();
        categories
            .Setup(x => x.SlugExistsAsync("phones", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        categories
            .Setup(x => x.ExistsAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CreateCategoryCommand("Phones", "phones", ParentCategoryId: parentId),
                CancellationToken.None));

        Assert.Contains("Category", exception.Message);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenParentExists_AddsChildCategoryAndSaves()
    {
        var (handler, categories, unitOfWork) = CreateSut();
        var parentId = Guid.NewGuid();
        categories
            .Setup(x => x.SlugExistsAsync("phones", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        categories
            .Setup(x => x.ExistsAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await handler.Handle(
            new CreateCategoryCommand("Phones", "phones", ParentCategoryId: parentId),
            CancellationToken.None);

        categories.Verify(
            x => x.AddAsync(It.Is<Category>(c => c.ParentCategoryId == parentId), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
