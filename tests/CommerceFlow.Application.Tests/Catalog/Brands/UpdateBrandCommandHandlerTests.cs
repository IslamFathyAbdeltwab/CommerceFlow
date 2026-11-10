using CommerceFlow.Application.Catalog.Brands.Commands.UpdateBrand;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Brands;

public sealed class UpdateBrandCommandHandlerTests
{
    private static (UpdateBrandCommandHandler Handler, Mock<IBrandRepository> Brands, Mock<IUnitOfWork> UnitOfWork)
        CreateSut()
    {
        var brands = new Mock<IBrandRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new UpdateBrandCommandHandler(brands.Object, unitOfWork.Object);
        return (handler, brands, unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenBrandDoesNotExist_ThrowsNotFound()
    {
        var (handler, brands, unitOfWork) = CreateSut();
        var brandId = Guid.NewGuid();
        brands
            .Setup(x => x.GetByIdAsync(brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateBrandCommand(brandId, "Apple", "apple"), CancellationToken.None));

        Assert.Contains("Brand", exception.Message);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSlugOwnedByAnotherBrand_ThrowsConflict()
    {
        var (handler, brands, unitOfWork) = CreateSut();
        var brandId = Guid.NewGuid();
        var existing = Brand.Create("Apple", "apple");
        brands
            .Setup(x => x.GetByIdAsync(brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        brands
            .Setup(x => x.SlugExistsAsync("samsung", brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new UpdateBrandCommand(brandId, "Samsung", "Samsung"), CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSlugExistsButIsOwn_UpdatesDetailsAndSaves()
    {
        var (handler, brands, unitOfWork) = CreateSut();
        var brandId = Guid.NewGuid();
        var existing = Brand.Create("Old Name", "old-slug");
        brands
            .Setup(x => x.GetByIdAsync(brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        brands
            .Setup(x => x.SlugExistsAsync("new-slug", brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await handler.Handle(
            new UpdateBrandCommand(brandId, "New Name", "New-Slug", "Updated", "https://logo.png"),
            CancellationToken.None);

        brands.Verify(
            x => x.SlugExistsAsync("new-slug", brandId, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal("New Name", existing.Name);
        Assert.Equal("new-slug", existing.Slug);
        Assert.Equal("Updated", existing.Description);
        Assert.Equal("https://logo.png", existing.LogoUrl);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
