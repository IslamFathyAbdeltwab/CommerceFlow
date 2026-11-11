using CommerceFlow.Application.Wishlists.Interfaces;
using CommerceFlow.Application.Wishlists.Queries.GetWishlistByCustomerId;
using CommerceFlow.Application.Wishlists.Queries.GetWishlistById;
using CommerceFlow.Application.Wishlists.Queries.GetWishlists;
using CommerceFlow.Domain.Entities.Wishlists;
using Moq;

namespace CommerceFlow.Application.Tests.Wishlists;

public sealed class WishlistQueryHandlerTests
{
    private readonly Mock<IWishlistRepository> _wishlists = new();

    // ---------- GetWishlistById ----------

    [Fact]
    public async Task GetWishlistById_WhenWishlistDoesNotExist_ReturnsNull()
    {
        var handler = new GetWishlistByIdQueryHandler(_wishlists.Object);
        _wishlists
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        var result = await handler.Handle(
            new GetWishlistByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetWishlistById_WhenWishlistExists_MapsItemsAndDerivedCount()
    {
        var handler = new GetWishlistByIdQueryHandler(_wishlists.Object);
        var variant1 = Guid.NewGuid();
        var variant2 = Guid.NewGuid();
        var wishlist = Wishlist.Create(WishlistFixtures.CustomerId);
        wishlist.AddItem(variant1);
        wishlist.AddItem(variant2);
        _wishlists
            .Setup(x => x.GetByIdAsync(wishlist.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wishlist);

        var result = await handler.Handle(
            new GetWishlistByIdQuery(wishlist.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(wishlist.Id, result.Id);
        Assert.Equal(WishlistFixtures.CustomerId, result.CustomerId);
        Assert.Equal(2, result.ItemCount); // derived, mapped
        Assert.Equal(wishlist.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(wishlist.UpdatedAtUtc, result.UpdatedAtUtc);

        Assert.Equal(2, result.Items.Count);
        var entityItems = wishlist.Items.ToList();
        Assert.Equal(entityItems[0].Id, result.Items[0].Id);
        Assert.Equal(variant1, result.Items[0].ProductVariantId);
        Assert.Equal(entityItems[0].AddedAtUtc, result.Items[0].AddedAtUtc);
        Assert.Equal(entityItems[1].Id, result.Items[1].Id);
        Assert.Equal(variant2, result.Items[1].ProductVariantId);
    }

    // ---------- GetWishlistByCustomerId ----------

    [Fact]
    public async Task GetWishlistByCustomer_WhenCustomerHasNoWishlist_ReturnsNull()
    {
        var handler = new GetWishlistByCustomerIdQueryHandler(_wishlists.Object);
        var customerId = Guid.NewGuid();
        _wishlists
            .Setup(x => x.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        var result = await handler.Handle(
            new GetWishlistByCustomerIdQuery(customerId), CancellationToken.None);

        Assert.Null(result); // "no wishlist yet" - distinct from 404 at the controller
    }

    [Fact]
    public async Task GetWishlistByCustomer_WhenWishlistExists_MapsWishlist()
    {
        var handler = new GetWishlistByCustomerIdQueryHandler(_wishlists.Object);
        var wishlist = WishlistFixtures.CreateWishlist(items: 2);
        _wishlists
            .Setup(x => x.GetByCustomerIdAsync(wishlist.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wishlist);

        var result = await handler.Handle(
            new GetWishlistByCustomerIdQuery(wishlist.CustomerId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(wishlist.Id, result.Id);
        Assert.Equal(wishlist.CustomerId, result.CustomerId);
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(2, result.Items.Count);
    }

    // ---------- GetWishlists ----------

    [Fact]
    public async Task GetWishlists_ReturnsPagedListMappedFromRepository()
    {
        var handler = new GetWishlistsQueryHandler(_wishlists.Object);
        var wishlist1 = WishlistFixtures.CreateWishlist(items: 1);
        var wishlist2 = WishlistFixtures.CreateWishlist(items: 3, customerId: Guid.NewGuid());
        _wishlists
            .Setup(x => x.CountAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        _wishlists
            .Setup(x => x.GetAllAsync(
                It.IsAny<Guid?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Wishlist> { wishlist1, wishlist2 });

        var result = await handler.Handle(new GetWishlistsQuery(), CancellationToken.None);

        Assert.Equal(42, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(3, result.TotalPages); // ceil(42 / 20)
        Assert.Equal(2, result.Items.Count);

        var first = result.Items[0];
        Assert.Equal(wishlist1.Id, first.Id);
        Assert.Equal(wishlist1.CustomerId, first.CustomerId);
        Assert.Equal(1, first.ItemCount);
        Assert.Equal(wishlist1.UpdatedAtUtc, first.UpdatedAtUtc);

        var second = result.Items[1];
        Assert.Equal(wishlist2.Id, second.Id);
        Assert.Equal(3, second.ItemCount);
    }

    [Fact]
    public async Task GetWishlists_WithFilters_PassesThemThroughToRepository()
    {
        var handler = new GetWishlistsQueryHandler(_wishlists.Object);
        var customerId = Guid.NewGuid();
        var wishlist = WishlistFixtures.CreateWishlist(customerId: customerId);
        _wishlists
            .Setup(x => x.CountAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _wishlists
            .Setup(x => x.GetAllAsync(customerId, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Wishlist> { wishlist });

        var result = await handler.Handle(
            new GetWishlistsQuery(customerId, 2, 10), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(customerId, result.Items[0].CustomerId);
        _wishlists.Verify(
            x => x.CountAsync(customerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _wishlists.Verify(
            x => x.GetAllAsync(customerId, 2, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
