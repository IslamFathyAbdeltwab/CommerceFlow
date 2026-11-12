using CommerceFlow.Application.Addresses.Commands.CreateAddress;
using CommerceFlow.Application.Addresses.Commands.DeleteAddress;
using CommerceFlow.Application.Addresses.Commands.SetDefaultAddress;
using CommerceFlow.Application.Addresses.Commands.UpdateAddress;
using CommerceFlow.Application.Addresses.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Addresses;
using CommerceFlow.Domain.Exceptions;
using Moq;

namespace CommerceFlow.Application.Tests.Addresses;

public sealed class AddressCommandHandlerTests
{
    private readonly Mock<ICustomerAddressRepository> _addresses = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void SetupAddress(CustomerAddress address) =>
        _addresses
            .Setup(x => x.GetByIdAsync(address.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

    // ---------- CreateAddress ----------

    [Fact]
    public async Task Create_WhenCustomerHasNoAddresses_FirstAddressBecomesDefaultAndSaves()
    {
        var handler = new CreateAddressCommandHandler(_addresses.Object, _unitOfWork.Object);
        var customerId = Guid.NewGuid();
        _addresses
            .Setup(x => x.CountAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var id = await handler.Handle(
            AddressFixtures.ValidCreateCommand(customerId), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _addresses.Verify(
            x => x.AddAsync(
                It.Is<CustomerAddress>(a => a.Id == id
                    && a.CustomerId == customerId
                    && a.IsDefault),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithDefaultRequested_ClearsPreviousDefaultAndSaves()
    {
        var handler = new CreateAddressCommandHandler(_addresses.Object, _unitOfWork.Object);
        var customerId = Guid.NewGuid();
        var previousDefault = AddressFixtures.CreateAddress(customerId, isDefault: true);
        _addresses
            .Setup(x => x.CountAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2); // not the first address
        _addresses
            .Setup(x => x.GetDefaultAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousDefault);

        var command = AddressFixtures.ValidCreateCommand(customerId) with { IsDefault = true };
        var id = await handler.Handle(command, CancellationToken.None);

        Assert.False(previousDefault.IsDefault); // cleared in the same SaveChanges
        _addresses.Verify(
            x => x.AddAsync(
                It.Is<CustomerAddress>(a => a.Id == id && a.IsDefault),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithoutDefaultFlagAndNotFirst_AddsNonDefaultAddressAndSaves()
    {
        var handler = new CreateAddressCommandHandler(_addresses.Object, _unitOfWork.Object);
        var customerId = Guid.NewGuid();
        _addresses
            .Setup(x => x.CountAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // not first, not default

        var id = await handler.Handle(
            AddressFixtures.ValidCreateCommand(customerId), CancellationToken.None);

        _addresses.Verify(
            x => x.AddAsync(
                It.Is<CustomerAddress>(a => a.Id == id && !a.IsDefault),
                It.IsAny<CancellationToken>()),
            Times.Once);
        // nothing to clear: no default lookup needed
        _addresses.Verify(
            x => x.GetDefaultAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- UpdateAddress ----------

    [Fact]
    public async Task Update_WhenAddressDoesNotExist_ThrowsNotFound()
    {
        var handler = new UpdateAddressCommandHandler(_addresses.Object, _unitOfWork.Object);
        var addressId = Guid.NewGuid();
        _addresses
            .Setup(x => x.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerAddress?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(AddressFixtures.ValidUpdateCommand(addressId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenAddressExists_UpdatesFieldsAndSaves()
    {
        var handler = new UpdateAddressCommandHandler(_addresses.Object, _unitOfWork.Object);
        var address = AddressFixtures.CreateAddress();
        SetupAddress(address);

        await handler.Handle(
            AddressFixtures.ValidUpdateCommand(address.Id), CancellationToken.None);

        Assert.Equal("Jane Doe", address.FullName);
        Assert.Equal("2 Second Street", address.Line1);
        Assert.Equal("Alexandria", address.City);
        Assert.Equal("23456", address.PostalCode);
        Assert.Equal("Floor 3", address.Line2);
        Assert.Equal("AL", address.State);
        Assert.True(address.UpdatedAtUtc >= address.CreatedAtUtc);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithMissingRequiredField_ThrowsDomainExceptionWithoutSaving()
    {
        var handler = new UpdateAddressCommandHandler(_addresses.Object, _unitOfWork.Object);
        var address = AddressFixtures.CreateAddress();
        SetupAddress(address);

        // domain guard is the safety net when the validator somehow lets this through
        var badCommand = AddressFixtures.ValidUpdateCommand(address.Id) with { FullName = "" };

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(badCommand, CancellationToken.None));

        Assert.Equal("John Doe", address.FullName); // validate-then-assign: nothing changed
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- SetDefaultAddress ----------

    [Fact]
    public async Task SetDefault_WhenAddressDoesNotExist_ThrowsNotFound()
    {
        var handler = new SetDefaultAddressCommandHandler(_addresses.Object, _unitOfWork.Object);
        var addressId = Guid.NewGuid();
        _addresses
            .Setup(x => x.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerAddress?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new SetDefaultAddressCommand(addressId), CancellationToken.None));

        _addresses.Verify(
            x => x.GetDefaultAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetDefault_WhenNotDefault_ClearsPreviousDefaultAndSetsIt()
    {
        var handler = new SetDefaultAddressCommandHandler(_addresses.Object, _unitOfWork.Object);
        var target = AddressFixtures.CreateAddress(); // not default
        var previousDefault = AddressFixtures.CreateAddress(isDefault: true);
        SetupAddress(target);
        _addresses
            .Setup(x => x.GetDefaultAsync(target.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousDefault);

        await handler.Handle(new SetDefaultAddressCommand(target.Id), CancellationToken.None);

        Assert.True(target.IsDefault);
        Assert.False(previousDefault.IsDefault); // one default per customer
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetDefault_WhenAlreadyDefault_DoesNotQueryPreviousDefault()
    {
        var handler = new SetDefaultAddressCommandHandler(_addresses.Object, _unitOfWork.Object);
        var target = AddressFixtures.CreateAddress(isDefault: true);
        SetupAddress(target);

        await handler.Handle(new SetDefaultAddressCommand(target.Id), CancellationToken.None);

        Assert.True(target.IsDefault);
        _addresses.Verify(
            x => x.GetDefaultAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never); // already default: nothing to clear
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- DeleteAddress ----------

    [Fact]
    public async Task Delete_WhenAddressDoesNotExist_ThrowsNotFound()
    {
        var handler = new DeleteAddressCommandHandler(_addresses.Object, _unitOfWork.Object);
        var addressId = Guid.NewGuid();
        _addresses
            .Setup(x => x.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerAddress?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteAddressCommand(addressId), CancellationToken.None));

        _addresses.Verify(x => x.Remove(It.IsAny<CustomerAddress>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenAddressExists_RemovesItAndSaves()
    {
        var handler = new DeleteAddressCommandHandler(_addresses.Object, _unitOfWork.Object);
        var address = AddressFixtures.CreateAddress(isDefault: true);
        SetupAddress(address);

        await handler.Handle(new DeleteAddressCommand(address.Id), CancellationToken.None);

        _addresses.Verify(x => x.Remove(address), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
