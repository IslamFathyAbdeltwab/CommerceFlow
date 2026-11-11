using CommerceFlow.Application.Orders.Commands.AddOrderLine;
using CommerceFlow.Application.Orders.Commands.ApplyDiscount;
using CommerceFlow.Application.Orders.Commands.CancelOrder;
using CommerceFlow.Application.Orders.Commands.PlaceOrder;
using CommerceFlow.Application.Orders.Commands.SetShippingFee;
using CommerceFlow.Application.Orders.Commands.UpdateOrderLineQuantity;
using CommerceFlow.Application.Orders.Queries.GetOrders;

namespace CommerceFlow.Application.Tests.Orders;

public sealed class OrderValidatorTests
{
    private static PlaceOrderAddressDto ValidAddress() =>
        new("John Doe", "1 Main Street", "Cairo", "12345", "EG");

    private static PlaceOrderLineDto Line(
        Guid? productVariantId = null,
        string sku = "SKU-1",
        string productName = "iPhone 15",
        decimal unitPrice = 100m,
        int quantity = 1) =>
        new(productVariantId ?? Guid.NewGuid(), sku, productName, unitPrice, quantity);

    private static PlaceOrderCommand ValidPlaceOrderCommand() =>
        new(
            CustomerId: Guid.NewGuid(),
            Lines: new List<PlaceOrderLineDto> { Line() },
            ShippingAddress: ValidAddress());

    // ---------- PlaceOrder ----------

    [Fact]
    public void PlaceOrder_WithValidInput_IsValid()
    {
        var result = new PlaceOrderCommandValidator().Validate(ValidPlaceOrderCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void PlaceOrder_WithEmptyCustomerId_IsInvalid()
    {
        var result = new PlaceOrderCommandValidator()
            .Validate(ValidPlaceOrderCommand() with { CustomerId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PlaceOrderCommand.CustomerId));
    }

    [Fact]
    public void PlaceOrder_WithEmptyLines_IsInvalid()
    {
        var result = new PlaceOrderCommandValidator()
            .Validate(ValidPlaceOrderCommand() with { Lines = new List<PlaceOrderLineDto>() });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PlaceOrderCommand.Lines));
    }

    [Fact]
    public void PlaceOrder_WithDuplicateVariantLines_IsInvalid()
    {
        var variantId = Guid.NewGuid();
        var result = new PlaceOrderCommandValidator()
            .Validate(ValidPlaceOrderCommand() with
            {
                Lines = new List<PlaceOrderLineDto> { Line(variantId), Line(variantId) },
            });

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage == "Order must not contain duplicate variants; use one line per variant.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void PlaceOrder_WithOutOfRangeLineQuantity_IsInvalid(int quantity)
    {
        var result = new PlaceOrderCommandValidator()
            .Validate(ValidPlaceOrderCommand() with
            {
                Lines = new List<PlaceOrderLineDto> { Line(quantity: quantity) },
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Lines[0].Quantity");
    }

    [Fact]
    public void PlaceOrder_WithShortCurrency_IsInvalid()
    {
        var result = new PlaceOrderCommandValidator()
            .Validate(ValidPlaceOrderCommand() with { Currency = "US" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PlaceOrderCommand.Currency));
    }

    [Fact]
    public void PlaceOrder_WithNegativeShippingFee_IsInvalid()
    {
        var result = new PlaceOrderCommandValidator()
            .Validate(ValidPlaceOrderCommand() with { ShippingFee = -1m });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PlaceOrderCommand.ShippingFee));
    }

    [Fact]
    public void PlaceOrder_WithNullAddress_IsInvalid()
    {
        var result = new PlaceOrderCommandValidator()
            .Validate(ValidPlaceOrderCommand() with { ShippingAddress = null! });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PlaceOrderCommand.ShippingAddress));
    }

    [Fact]
    public void PlaceOrder_WithEmptyAddressFullName_IsInvalid()
    {
        var result = new PlaceOrderCommandValidator()
            .Validate(ValidPlaceOrderCommand() with
            {
                ShippingAddress = ValidAddress() with { FullName = "" },
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ShippingAddress.FullName");
    }

    [Fact]
    public void PlaceOrder_WithTooLongCustomerNote_IsInvalid()
    {
        var result = new PlaceOrderCommandValidator()
            .Validate(ValidPlaceOrderCommand() with { CustomerNote = new string('x', 1001) });

        Assert.False(result.IsValid);
    }

    // ---------- AddOrderLine ----------

    [Fact]
    public void AddOrderLine_WithValidInput_IsValid()
    {
        var result = new AddOrderLineCommandValidator()
            .Validate(new AddOrderLineCommand(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "iPhone 15", 100m, 2));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void AddOrderLine_WithEmptyIds_IsInvalid()
    {
        var result = new AddOrderLineCommandValidator()
            .Validate(new AddOrderLineCommand(Guid.Empty, Guid.Empty, "SKU-1", "iPhone 15", 100m, 2));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddOrderLineCommand.OrderId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddOrderLineCommand.ProductVariantId));
    }

    [Fact]
    public void AddOrderLine_WithEmptySku_IsInvalid()
    {
        var result = new AddOrderLineCommandValidator()
            .Validate(new AddOrderLineCommand(Guid.NewGuid(), Guid.NewGuid(), "", "iPhone 15", 100m, 2));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddOrderLineCommand.Sku));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void AddOrderLine_WithOutOfRangeQuantity_IsInvalid(int quantity)
    {
        var result = new AddOrderLineCommandValidator()
            .Validate(new AddOrderLineCommand(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "iPhone 15", 100m, quantity));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddOrderLineCommand.Quantity));
    }

    [Fact]
    public void AddOrderLine_WithNegativeUnitPrice_IsInvalid()
    {
        var result = new AddOrderLineCommandValidator()
            .Validate(new AddOrderLineCommand(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "iPhone 15", -1m, 2));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddOrderLineCommand.UnitPrice));
    }

    // ---------- UpdateOrderLineQuantity ----------

    [Fact]
    public void UpdateLineQuantity_WithValidInput_IsValid()
    {
        var result = new UpdateOrderLineQuantityCommandValidator()
            .Validate(new UpdateOrderLineQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), 3));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void UpdateLineQuantity_WithOutOfRangeQuantity_IsInvalid(int quantity)
    {
        var result = new UpdateOrderLineQuantityCommandValidator()
            .Validate(new UpdateOrderLineQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), quantity));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateOrderLineQuantityCommand.Quantity));
    }

    [Fact]
    public void UpdateLineQuantity_WithEmptyIds_IsInvalid()
    {
        var result = new UpdateOrderLineQuantityCommandValidator()
            .Validate(new UpdateOrderLineQuantityCommand(Guid.Empty, Guid.Empty, 3));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateOrderLineQuantityCommand.OrderId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateOrderLineQuantityCommand.OrderLineId));
    }

    // ---------- SetShippingFee ----------

    [Fact]
    public void SetShippingFee_WithValidInput_IsValid()
    {
        var result = new SetShippingFeeCommandValidator()
            .Validate(new SetShippingFeeCommand(Guid.NewGuid(), 25m));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SetShippingFee_WithNegativeFee_IsInvalid()
    {
        var result = new SetShippingFeeCommandValidator()
            .Validate(new SetShippingFeeCommand(Guid.NewGuid(), -1m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SetShippingFeeCommand.ShippingFee));
    }

    // ---------- ApplyDiscount ----------

    [Fact]
    public void ApplyDiscount_WithValidInput_IsValid()
    {
        var result = new ApplyDiscountCommandValidator()
            .Validate(new ApplyDiscountCommand(Guid.NewGuid(), 50m));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ApplyDiscount_WithNegativeAmount_IsInvalid()
    {
        var result = new ApplyDiscountCommandValidator()
            .Validate(new ApplyDiscountCommand(Guid.NewGuid(), -5m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ApplyDiscountCommand.DiscountAmount));
    }

    // ---------- CancelOrder ----------

    [Fact]
    public void CancelOrder_WithValidInput_IsValid()
    {
        var result = new CancelOrderCommandValidator()
            .Validate(new CancelOrderCommand(Guid.NewGuid(), "Customer request"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CancelOrder_WithNullReason_IsValid()
    {
        var result = new CancelOrderCommandValidator()
            .Validate(new CancelOrderCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CancelOrder_WithTooLongReason_IsInvalid()
    {
        var result = new CancelOrderCommandValidator()
            .Validate(new CancelOrderCommand(Guid.NewGuid(), new string('x', 501)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CancelOrderCommand.Reason));
    }

    // ---------- GetOrders ----------

    [Fact]
    public void GetOrders_WithDefaultPaging_IsValid()
    {
        var result = new GetOrdersQueryValidator().Validate(new GetOrdersQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetOrders_WithOutOfRangePaging_IsInvalid()
    {
        var result = new GetOrdersQueryValidator()
            .Validate(new GetOrdersQuery(PageNumber: 0, PageSize: 101));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetOrdersQuery.PageNumber));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetOrdersQuery.PageSize));
    }

    [Fact]
    public void GetOrders_WithTooLongSearchTerm_IsInvalid()
    {
        var result = new GetOrdersQueryValidator()
            .Validate(new GetOrdersQuery(SearchTerm: new string('x', 201)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetOrdersQuery.SearchTerm));
    }
}
