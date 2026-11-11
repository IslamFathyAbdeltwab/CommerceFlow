using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Wishlists.Commands.AddWishlistItem;
using CommerceFlow.Application.Wishlists.Commands.ClearWishlist;
using CommerceFlow.Application.Wishlists.Commands.CreateWishlist;
using CommerceFlow.Application.Wishlists.Commands.RemoveWishlistItem;
using CommerceFlow.Application.Wishlists.Interfaces;
using CommerceFlow.Domain.Entities.Wishlists;
using Moq;

namespace CommerceFlow.Application.Tests.Wishlists;

public sealed class WishlistCommandHandlerTests
{
    private readonly Mock<IWishlistRepository> _wishlists = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void SetupWishlist(Wishlist wishlist) =>
        _wishlists
            .Setup(x => x.GetByIdAsync(wishlist.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wishlist);

    // ---------- CreateWishlist ----------

    [Fact]
    public async Task CreateWishlist_WhenCustomerAlreadyHasWishlist_ThrowsConflict()
    {
        var handler = new CreateWishlistCommandHandler(_wishlists.Object, _unitOfWork.Object);
        var customerId = Guid.NewGuid();
        _wishlists
            .Setup(x => x.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WishlistFixtures.CreateWishlist());

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateWishlistCommand(customerId), CancellationToken.None));

        Assert.Contains($"Customer '{customerId}' already has a wishlist.", exception.Message);
        _wishlists.Verify(x => x.AddAsync(It.IsAny<Wishlist>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateWishlist_WhenCustomerHasNone_CreatesWishlistAndSaves()
    {
        var handler = new CreateWishlistCommandHandler(_wishlists.Object, _unitOfWork.Object);
        var customerId = Guid.NewGuid();
        _wishlists
            .Setup(x => x.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        var id = await handler.Handle(new CreateWishlistCommand(customerId), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _wishlists.Verify(
            x => x.AddAsync(
                It.Is<Wishlist>(w => w.Id == id
                    && w.CustomerId == customerId
                    && w.ItemCount == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- AddWishlistItem ----------

    [Fact]
    public async Task AddItem_WhenWishlistDoesNotExist_ThrowsNotFound()
    {
        var handler = new AddWishlistItemCommandHandler(_wishlists.Object, _unitOfWork.Object);
        var wishlistId = Guid.NewGuid();
        _wishlists
            .Setup(x => x.GetByIdAsync(wishlistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new AddWishlistItemCommand(wishlistId, Guid.NewGuid()), CancellationToken.None));

        Assert.Contains("Wishlist", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddItem_WhenVariantNotSaved_AddsItemAndSaves()
    {
        var handler = new AddWishlistItemCommandHandler(_wishlists.Object, _unitOfWork.Object);
        var wishlist = WishlistFixtures.CreateWishlist();
        SetupWishlist(wishlist);
        var variantId = Guid.NewGuid();

        var itemId = await handler.Handle(
            new AddWishlistItemCommand(wishlist.Id, variantId), CancellationToken.None);

        var item = Assert.Single(wishlist.Items);
        Assert.Equal(itemId, item.Id);
        Assert.Equal(variantId, item.ProductVariantId);
        Assert.Equal(wishlist.Id, item.WishlistId);
        Assert.Equal(1, wishlist.ItemCount);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddItem_WhenVariantAlreadySaved_ReturnsSameItemIdWithoutDuplicating()
    {
        var handler = new AddWishlistItemCommandHandler(_wishlists.Object, _unitOfWork.Object);
        var variantId = Guid.NewGuid();
        var wishlist = WishlistFixtures.CreateWishlist();
        var existing = wishlist.AddItem(variantId);
        SetupWishlist(wishlist);

        var itemId = await handler.Handle(
            new AddWishlistItemCommand(wishlist.Id, variantId), CancellationToken.None);

        Assert.Equal(existing.Id, itemId); // idempotent: the same item id comes back
        Assert.Single(wishlist.Items);
        Assert.Equal(1, wishlist.ItemCount);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddItem_WhenWishlistAtMaxItems_ThrowsConflict()
    {
        var handler = new AddWishlistItemCommandHandler(_wishlists.Object, _unitOfWork.Object);
        var (wishlist, _) = WishlistFixtures.WishlistAtMaxItems();
        SetupWishlist(wishlist);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new AddWishlistItemCommand(wishlist.Id, Guid.NewGuid()), CancellationToken.None));

        Assert.Contains("A wishlist cannot have more than 100 items.", exception.Message);
        Assert.Equal(Wishlist.MaxItems, wishlist.ItemCount); // untouched
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddItem_WhenAtMaxItemsButVariantIsSaved_ReturnsExistingItem()
    {
        var handler = new AddWishlistItemCommandHandler(_wishlists.Object, _unitOfWork.Object);
        var (wishlist, variantId) = WishlistFixtures.WishlistAtMaxItems();
        SetupWishlist(wishlist);

        var itemId = await handler.Handle(
            new AddWishlistItemCommand(wishlist.Id, variantId), CancellationToken.None);

        var existing = wishlist.Items.Single(i => i.ProductVariantId == variantId);
        Assert.Equal(existing.Id, itemId); // idempotent path wins over the cap
        Assert.Equal(Wishlist.MaxItems, wishlist.ItemCount); // no new item
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- RemoveWishlistItem ----------

    [Fact]
    public async Task RemoveItem_WhenWishlistDoesNotExist_ThrowsNotFound()
    {
        var handler = new RemoveWishlistItemCommandHandler(_wishlists.Object, _unitOfWork.Object);
        var wishlistId = Guid.NewGuid();
        _wishlists
            .Setup(x => x.GetByIdAsync(wishlistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new RemoveWishlistItemCommand(wishlistId, Guid.NewGuid()), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveItem_WhenItemDoesNotExist_ThrowsNotFound()
    {
        var handler = new RemoveWishlistItemCommandHandler(_wishlists.Object, _unitOfWork.Object);
        var (wishlist, _) = WishlistFixtures.WishlistWithItem();
        SetupWishlist(wishlist);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new RemoveWishlistItemCommand(wishlist.Id, Guid.NewGuid()), CancellationToken.None));

        Assert.Contains("WishlistItem", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveItem_WhenItemExists_RemovesItAndSaves()
    {
        var handler = new RemoveWishlistItemCommandHandler(_wishlists.Object, _unitOfWork.Object);
        var (wishlist, item) = WishlistFixtures.WishlistWithItem();
        SetupWishlist(wishlist);

        await handler.Handle(
            new RemoveWishlistItemCommand(wishlist.Id, item.Id), CancellationToken.None);

        Assert.Empty(wishlist.Items);
        Assert.Equal(0, wishlist.ItemCount);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- ClearWishlist ----------

    [Fact]
    public async Task Clear_WhenWishlistDoesNotExist_ThrowsNotFound()
    {
        var handler = new ClearWishlistCommandHandler(_wishlists.Object, _unitOfWork.Object);
        var wishlistId = Guid.NewGuid();
        _wishlists
            .Setup(x => x.GetByIdAsync(wishlistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ClearWishlistCommand(wishlistId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Clear_WhenWishlistHasItems_ClearsAndSaves()
    {
        var handler = new ClearWishlistCommandHandler(_wishlists.Object, _unitOfWork.Object);
        var wishlist = WishlistFixtures.CreateWishlist(items: 3);
        SetupWishlist(wishlist);

        await handler.Handle(new ClearWishlistCommand(wishlist.Id), CancellationToken.None);

        Assert.Empty(wishlist.Items);
        Assert.Equal(0, wishlist.ItemCount);
        Assert.Equal(WishlistFixtures.CustomerId, wishlist.CustomerId); // cleared, not deleted
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
