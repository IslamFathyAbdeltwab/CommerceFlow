using CommerceFlow.Application.Addresses.Interfaces;
using CommerceFlow.Application.Addresses.Queries.GetAddressById;
using CommerceFlow.Application.Addresses.Queries.GetAddresses;
using CommerceFlow.Application.Addresses.Queries.GetDefaultAddress;
using CommerceFlow.Domain.Entities.Addresses;
using Moq;

namespace CommerceFlow.Application.Tests.Addresses;

public sealed class AddressQueryHandlerTests
{
    private readonly Mock<ICustomerAddressRepository> _addresses = new();

    // ---------- GetAddressById ----------

    [Fact]
    public async Task GetAddressById_WhenAddressDoesNotExist_ReturnsNull()
    {
        var handler = new GetAddressByIdQueryHandler(_addresses.Object);
        _addresses
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerAddress?)null);

        var result = await handler.Handle(
            new GetAddressByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAddressById_WhenAddressExists_MapsAllFields()
    {
        var handler = new GetAddressByIdQueryHandler(_addresses.Object);
        var address = AddressFixtures.CreateAddress(isDefault: true);
        _addresses
            .Setup(x => x.GetByIdAsync(address.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        var result = await handler.Handle(
            new GetAddressByIdQuery(address.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(address.Id, result.Id);
        Assert.Equal(AddressFixtures.CustomerId, result.CustomerId);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("1 Main Street", result.Line1);
        Assert.Equal("Apt 4B", result.Line2);
        Assert.Equal("Cairo", result.City);
        Assert.Equal("CA", result.State);
        Assert.Equal("12345", result.PostalCode);
        Assert.Equal("EG", result.Country);
        Assert.Equal("01000000000", result.PhoneNumber);
        Assert.True(result.IsDefault);
        Assert.Equal(address.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(address.UpdatedAtUtc, result.UpdatedAtUtc);
    }

    // ---------- GetDefaultAddress ----------

    [Fact]
    public async Task GetDefaultAddress_WhenCustomerHasNoDefault_ReturnsNull()
    {
        var handler = new GetDefaultAddressQueryHandler(_addresses.Object);
        var customerId = Guid.NewGuid();
        _addresses
            .Setup(x => x.GetDefaultAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerAddress?)null);

        var result = await handler.Handle(
            new GetDefaultAddressQuery(customerId), CancellationToken.None);

        Assert.Null(result); // e.g. right after deleting the default
    }

    [Fact]
    public async Task GetDefaultAddress_WhenDefaultExists_MapsIt()
    {
        var handler = new GetDefaultAddressQueryHandler(_addresses.Object);
        var address = AddressFixtures.CreateAddress(isDefault: true);
        _addresses
            .Setup(x => x.GetDefaultAsync(address.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        var result = await handler.Handle(
            new GetDefaultAddressQuery(address.CustomerId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(address.Id, result.Id);
        Assert.True(result.IsDefault);
        Assert.Equal("John Doe", result.FullName);
    }

    // ---------- GetAddresses ----------

    [Fact]
    public async Task GetAddresses_ReturnsPagedListMappedFromRepository()
    {
        var handler = new GetAddressesQueryHandler(_addresses.Object);
        var address1 = AddressFixtures.CreateAddress(isDefault: true);
        var address2 = AddressFixtures.CreateAddress(customerId: Guid.NewGuid());
        _addresses
            .Setup(x => x.CountAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);
        _addresses
            .Setup(x => x.GetAllAsync(
                It.IsAny<Guid?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomerAddress> { address1, address2 });

        var result = await handler.Handle(new GetAddressesQuery(), CancellationToken.None);

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(2, result.TotalPages); // ceil(25 / 20)
        Assert.Equal(2, result.Items.Count);

        var first = result.Items[0];
        Assert.Equal(address1.Id, first.Id);
        Assert.Equal(address1.CustomerId, first.CustomerId);
        Assert.Equal("John Doe", first.FullName);
        Assert.True(first.IsDefault);
        Assert.Equal(address1.UpdatedAtUtc, first.UpdatedAtUtc);

        Assert.Equal(address2.Id, result.Items[1].Id);
        Assert.False(result.Items[1].IsDefault);
    }

    [Fact]
    public async Task GetAddresses_WithFilters_PassesThemThroughToRepository()
    {
        var handler = new GetAddressesQueryHandler(_addresses.Object);
        var customerId = Guid.NewGuid();
        var address = AddressFixtures.CreateAddress(customerId);
        _addresses
            .Setup(x => x.CountAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _addresses
            .Setup(x => x.GetAllAsync(customerId, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomerAddress> { address });

        var result = await handler.Handle(
            new GetAddressesQuery(customerId, 2, 10), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(customerId, result.Items[0].CustomerId);
        _addresses.Verify(
            x => x.CountAsync(customerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _addresses.Verify(
            x => x.GetAllAsync(customerId, 2, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
