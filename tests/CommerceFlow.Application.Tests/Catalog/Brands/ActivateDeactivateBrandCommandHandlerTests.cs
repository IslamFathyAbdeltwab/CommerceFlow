using CommerceFlow.Application.Catalog.Brands.Commands.ActivateBrand;
using CommerceFlow.Application.Catalog.Brands.Commands.DeactivateBrand;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using Moq;

namespace CommerceFlow.Application.Tests.Catalog.Brands;

public sealed class ActivateDeactivateBrandCommandHandlerTests
{
    private readonly Mock<IBrandRepository> _brands = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Activate_WhenBrandDoesNotExist_ThrowsNotFound()
    {
        var handler = new ActivateBrandCommandHandler(_brands.Object, _unitOfWork.Object);
        var brandId = Guid.NewGuid();
        _brands
            .Setup(x => x.GetByIdAsync(brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ActivateBrandCommand(brandId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Activate_WhenBrandIsInactive_ActivatesAndSaves()
    {
        var handler = new ActivateBrandCommandHandler(_brands.Object, _unitOfWork.Object);
        var brand = Brand.Create("Apple", "apple");
        brand.Deactivate();
        _brands
            .Setup(x => x.GetByIdAsync(brand.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(brand);

        await handler.Handle(new ActivateBrandCommand(brand.Id), CancellationToken.None);

        Assert.True(brand.IsActive);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deactivate_WhenBrandDoesNotExist_ThrowsNotFound()
    {
        var handler = new DeactivateBrandCommandHandler(_brands.Object, _unitOfWork.Object);
        var brandId = Guid.NewGuid();
        _brands
            .Setup(x => x.GetByIdAsync(brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeactivateBrandCommand(brandId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deactivate_WhenBrandIsActive_DeactivatesAndSaves()
    {
        var handler = new DeactivateBrandCommandHandler(_brands.Object, _unitOfWork.Object);
        var brand = Brand.Create("Apple", "apple"); // new brands start active
        _brands
            .Setup(x => x.GetByIdAsync(brand.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(brand);

        await handler.Handle(new DeactivateBrandCommand(brand.Id), CancellationToken.None);

        Assert.False(brand.IsActive);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
