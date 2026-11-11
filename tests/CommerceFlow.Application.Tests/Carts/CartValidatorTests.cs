using CommerceFlow.Application.Carts.Commands.AbandonCart;
using CommerceFlow.Application.Carts.Commands.AddCartItem;
using CommerceFlow.Application.Carts.Commands.ChangeCartItemQuantity;
using CommerceFlow.Application.Carts.Commands.CheckoutCart;
using CommerceFlow.Application.Carts.Commands.ClearCart;
using CommerceFlow.Application.Carts.Commands.CreateCart;
using CommerceFlow.Application.Carts.Commands.RemoveCartItem;
using CommerceFlow.Application.Carts.Queries.GetCarts;
using CommerceFlow.Domain.Entities.Carts;

namespace CommerceFlow.Application.Tests.Carts;

public sealed class CartValidatorTests
{
    // ---------- CreateCart ----------

    [Fact]
    public void CreateCart_WithValidInput_IsValid()
    {
        var result = new CreateCartCommandValidator()
            .Validate(new CreateCartCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateCart_WithEmptyCustomerId_IsInvalid()
    {
        var result = new CreateCartCommandValidator()
            .Validate(new CreateCartCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCartCommand.CustomerId));
    }

    // ---------- AddCartItem ----------

    private static AddCartItemCommand ValidAddItemCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 1);

    [Fact]
    public void AddCartItem_WithValidInput_IsValid()
    {
        var result = new AddCartItemCommandValidator().Validate(ValidAddItemCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void AddCartItem_WithEmptyCartId_IsInvalid()
    {
        var result = new AddCartItemCommandValidator()
            .Validate(ValidAddItemCommand() with { CartId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddCartItemCommand.CartId));
    }

    [Fact]
    public void AddCartItem_WithEmptyProductVariantId_IsInvalid()
    {
        var result = new AddCartItemCommandValidator()
            .Validate(ValidAddItemCommand() with { ProductVariantId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(AddCartItemCommand.ProductVariantId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(CartLine.MaxQuantity + 1)]
    public void AddCartItem_WithOutOfRangeQuantity_IsInvalid(int quantity)
    {
        var result = new AddCartItemCommandValidator()
            .Validate(ValidAddItemCommand() with { Quantity = quantity });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddCartItemCommand.Quantity));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(CartLine.MaxQuantity)]
    public void AddCartItem_WithBoundaryQuantity_IsValid(int quantity)
    {
        var result = new AddCartItemCommandValidator()
            .Validate(ValidAddItemCommand() with { Quantity = quantity });

        Assert.True(result.IsValid);
    }

    // ---------- ChangeCartItemQuantity ----------

    private static ChangeCartItemQuantityCommand ValidChangeQuantityCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 5);

    [Fact]
    public void ChangeQuantity_WithValidInput_IsValid()
    {
        var result = new ChangeCartItemQuantityCommandValidator()
            .Validate(ValidChangeQuantityCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ChangeQuantity_WithEmptyCartId_IsInvalid()
    {
        var result = new ChangeCartItemQuantityCommandValidator()
            .Validate(ValidChangeQuantityCommand() with { CartId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(ChangeCartItemQuantityCommand.CartId));
    }

    [Fact]
    public void ChangeQuantity_WithEmptyCartLineId_IsInvalid()
    {
        var result = new ChangeCartItemQuantityCommandValidator()
            .Validate(ValidChangeQuantityCommand() with { CartLineId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(ChangeCartItemQuantityCommand.CartLineId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(CartLine.MaxQuantity + 1)]
    public void ChangeQuantity_WithOutOfRangeQuantity_IsInvalid(int quantity)
    {
        var result = new ChangeCartItemQuantityCommandValidator()
            .Validate(ValidChangeQuantityCommand() with { Quantity = quantity });

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(ChangeCartItemQuantityCommand.Quantity));
    }

    // ---------- RemoveCartItem ----------

    [Fact]
    public void RemoveItem_WithValidInput_IsValid()
    {
        var result = new RemoveCartItemCommandValidator()
            .Validate(new RemoveCartItemCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RemoveItem_WithEmptyCartId_IsInvalid()
    {
        var result = new RemoveCartItemCommandValidator()
            .Validate(new RemoveCartItemCommand(Guid.Empty, Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RemoveCartItemCommand.CartId));
    }

    [Fact]
    public void RemoveItem_WithEmptyCartLineId_IsInvalid()
    {
        var result = new RemoveCartItemCommandValidator()
            .Validate(new RemoveCartItemCommand(Guid.NewGuid(), Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(RemoveCartItemCommand.CartLineId));
    }

    // ---------- ClearCart ----------

    [Fact]
    public void ClearCart_WithValidInput_IsValid()
    {
        var result = new ClearCartCommandValidator()
            .Validate(new ClearCartCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ClearCart_WithEmptyCartId_IsInvalid()
    {
        var result = new ClearCartCommandValidator()
            .Validate(new ClearCartCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ClearCartCommand.CartId));
    }

    // ---------- CheckoutCart ----------

    [Fact]
    public void Checkout_WithValidInput_IsValid()
    {
        var result = new CheckoutCartCommandValidator()
            .Validate(new CheckoutCartCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Checkout_WithEmptyCartId_IsInvalid()
    {
        var result = new CheckoutCartCommandValidator()
            .Validate(new CheckoutCartCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CheckoutCartCommand.CartId));
    }

    // ---------- AbandonCart ----------

    [Fact]
    public void Abandon_WithValidInput_IsValid()
    {
        var result = new AbandonCartCommandValidator()
            .Validate(new AbandonCartCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Abandon_WithEmptyCartId_IsInvalid()
    {
        var result = new AbandonCartCommandValidator()
            .Validate(new AbandonCartCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AbandonCartCommand.CartId));
    }

    // ---------- GetCarts (query) ----------

    [Fact]
    public void GetCarts_WithDefaults_IsValid()
    {
        var result = new GetCartsQueryValidator().Validate(new GetCartsQuery());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void GetCarts_WithOutOfRangePageNumber_IsInvalid(int pageNumber)
    {
        var result = new GetCartsQueryValidator()
            .Validate(new GetCartsQuery(PageNumber: pageNumber));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetCartsQuery.PageNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void GetCarts_WithOutOfRangePageSize_IsInvalid(int pageSize)
    {
        var result = new GetCartsQueryValidator()
            .Validate(new GetCartsQuery(PageSize: pageSize));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetCartsQuery.PageSize));
    }
}
