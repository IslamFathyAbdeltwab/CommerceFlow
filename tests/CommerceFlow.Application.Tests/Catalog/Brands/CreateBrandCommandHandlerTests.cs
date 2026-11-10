using CommerceFlow.Application.Catalog.Brands.Commands.CreateBrand;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Brands;

public sealed class CreateBrandCommandHandlerTests
{
    private static (CreateBrandCommandHandler Handler, Mock<IBrandRepository> Brands, Mock<IUnitOfWork> UnitOfWork)
        CreateSut()
    {
        var brands = new Mock<IBrandRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateBrandCommandHandler(brands.Object, unitOfWork.Object);
        return (handler, brands, unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenSlugIsFree_AddsBrandAndSavesChanges()
    {
        var (handler, brands, unitOfWork) = CreateSut();
        brands
            .Setup(x => x.SlugExistsAsync("apple", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var id = await handler.Handle(
            new CreateBrandCommand("Apple", "apple", "Smartphones"),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        brands.Verify(
            x => x.AddAsync(
                It.Is<Brand>(b => b.Name == "Apple"
                    && b.Slug == "apple"
                    && b.Description == "Smartphones"
                    && b.IsActive),
                It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ThrowsConflictAndDoesNotSave()
    {
        var (handler, brands, unitOfWork) = CreateSut();
        brands
            .Setup(x => x.SlugExistsAsync("apple", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateBrandCommand("Apple", "Apple"), CancellationToken.None));

        Assert.Contains("'apple'", exception.Message);
        brands.Verify(x => x.AddAsync(It.IsAny<Brand>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSlugHasMixedCase_ChecksAgainstNormalizedSlug()
    {
        var (handler, brands, _) = CreateSut();
        brands
            .Setup(x => x.SlugExistsAsync("apple", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await handler.Handle(new CreateBrandCommand("Apple", "  Apple  "), CancellationToken.None);

        brands.Verify(
            x => x.SlugExistsAsync("apple", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
