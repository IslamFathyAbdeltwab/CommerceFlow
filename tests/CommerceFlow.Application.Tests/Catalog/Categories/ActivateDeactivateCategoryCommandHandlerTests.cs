using CommerceFlow.Application.Catalog.Categories.Commands.ActivateCategory;
using CommerceFlow.Application.Catalog.Categories.Commands.DeactivateCategory;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Categories;

public sealed class ActivateDeactivateCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Activate_WhenCategoryDoesNotExist_ThrowsNotFound()
    {
        var handler = new ActivateCategoryCommandHandler(_categories.Object, _unitOfWork.Object);
        var categoryId = Guid.NewGuid();
        _categories
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ActivateCategoryCommand(categoryId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Activate_WhenCategoryIsInactive_ActivatesAndSaves()
    {
        var handler = new ActivateCategoryCommandHandler(_categories.Object, _unitOfWork.Object);
        var category = Category.Create("Phones", "phones");
        category.Deactivate();
        _categories
            .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        await handler.Handle(new ActivateCategoryCommand(category.Id), CancellationToken.None);

        Assert.True(category.IsActive);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deactivate_WhenCategoryDoesNotExist_ThrowsNotFound()
    {
        var handler = new DeactivateCategoryCommandHandler(_categories.Object, _unitOfWork.Object);
        var categoryId = Guid.NewGuid();
        _categories
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeactivateCategoryCommand(categoryId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deactivate_WhenCategoryIsActive_DeactivatesAndSaves()
    {
        var handler = new DeactivateCategoryCommandHandler(_categories.Object, _unitOfWork.Object);
        var category = Category.Create("Phones", "phones"); // new categories start active
        _categories
            .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        await handler.Handle(new DeactivateCategoryCommand(category.Id), CancellationToken.None);

        Assert.False(category.IsActive);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
