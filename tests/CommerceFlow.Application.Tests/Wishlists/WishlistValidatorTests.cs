using CommerceFlow.Application.Wishlists.Commands.AddWishlistItem;
using CommerceFlow.Application.Wishlists.Commands.ClearWishlist;
using CommerceFlow.Application.Wishlists.Commands.CreateWishlist;
using CommerceFlow.Application.Wishlists.Commands.RemoveWishlistItem;
using CommerceFlow.Application.Wishlists.Queries.GetWishlists;

namespace CommerceFlow.Application.Tests.Wishlists;

public sealed class WishlistValidatorTests
{
    // ---------- CreateWishlist ----------

    [Fact]
    public void CreateWishlist_WithValidInput_IsValid()
    {
        var result = new CreateWishlistCommandValidator()
            .Validate(new CreateWishlistCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateWishlist_WithEmptyCustomerId_IsInvalid()
    {
        var result = new CreateWishlistCommandValidator()
            .Validate(new CreateWishlistCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWishlistCommand.CustomerId));
    }

    // ---------- AddWishlistItem ----------

    private static AddWishlistItemCommand ValidAddItemCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void AddItem_WithValidInput_IsValid()
    {
        var result = new AddWishlistItemCommandValidator().Validate(ValidAddItemCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void AddItem_WithEmptyWishlistId_IsInvalid()
    {
        var result = new AddWishlistItemCommandValidator()
            .Validate(ValidAddItemCommand() with { WishlistId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(AddWishlistItemCommand.WishlistId));
    }

    [Fact]
    public void AddItem_WithEmptyProductVariantId_IsInvalid()
    {
        var result = new AddWishlistItemCommandValidator()
            .Validate(ValidAddItemCommand() with { ProductVariantId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(AddWishlistItemCommand.ProductVariantId));
    }

    // ---------- RemoveWishlistItem ----------

    [Fact]
    public void RemoveItem_WithValidInput_IsValid()
    {
        var result = new RemoveWishlistItemCommandValidator()
            .Validate(new RemoveWishlistItemCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RemoveItem_WithEmptyWishlistId_IsInvalid()
    {
        var result = new RemoveWishlistItemCommandValidator()
            .Validate(new RemoveWishlistItemCommand(Guid.Empty, Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(RemoveWishlistItemCommand.WishlistId));
    }

    [Fact]
    public void RemoveItem_WithEmptyWishlistItemId_IsInvalid()
    {
        var result = new RemoveWishlistItemCommandValidator()
            .Validate(new RemoveWishlistItemCommand(Guid.NewGuid(), Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(RemoveWishlistItemCommand.WishlistItemId));
    }

    // ---------- ClearWishlist ----------

    [Fact]
    public void Clear_WithValidInput_IsValid()
    {
        var result = new ClearWishlistCommandValidator()
            .Validate(new ClearWishlistCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Clear_WithEmptyWishlistId_IsInvalid()
    {
        var result = new ClearWishlistCommandValidator()
            .Validate(new ClearWishlistCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ClearWishlistCommand.WishlistId));
    }

    // ---------- GetWishlists (query) ----------

    [Fact]
    public void GetWishlists_WithDefaults_IsValid()
    {
        var result = new GetWishlistsQueryValidator().Validate(new GetWishlistsQuery());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void GetWishlists_WithOutOfRangePageNumber_IsInvalid(int pageNumber)
    {
        var result = new GetWishlistsQueryValidator()
            .Validate(new GetWishlistsQuery(PageNumber: pageNumber));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetWishlistsQuery.PageNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void GetWishlists_WithOutOfRangePageSize_IsInvalid(int pageSize)
    {
        var result = new GetWishlistsQueryValidator()
            .Validate(new GetWishlistsQuery(PageSize: pageSize));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetWishlistsQuery.PageSize));
    }
}
