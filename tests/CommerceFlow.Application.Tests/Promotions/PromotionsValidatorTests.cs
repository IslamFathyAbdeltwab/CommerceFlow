using CommerceFlow.Application.Promotions.Commands.ActivatePromotion;
using CommerceFlow.Application.Promotions.Commands.CreatePromotion;
using CommerceFlow.Application.Promotions.Commands.ExpirePromotion;
using CommerceFlow.Application.Promotions.Commands.PausePromotion;
using CommerceFlow.Application.Promotions.Commands.RedeemPromotion;
using CommerceFlow.Application.Promotions.Commands.UpdatePromotion;
using CommerceFlow.Application.Promotions.Queries.GetPromotions;
using CommerceFlow.Domain.Entities.Promotions;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Tests.Promotions;

public sealed class PromotionsValidatorTests
{
    private static string TooLong(int length) => new('x', length);

    // ---------- CreatePromotion ----------

    [Fact]
    public void Create_WithValidInput_IsValid()
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCode_IsInvalid(string code)
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with { Code = code });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreatePromotionCommand.Code));
    }

    [Fact]
    public void Create_WithTooLongCode_IsInvalid()
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with
            {
                Code = TooLong(Promotion.CodeMaxLength + 1)
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreatePromotionCommand.Code));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_IsInvalid(string name)
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with { Name = name });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreatePromotionCommand.Name));
    }

    [Fact]
    public void Create_WithTooLongName_IsInvalid()
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with
            {
                Name = TooLong(Promotion.NameMaxLength + 1)
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreatePromotionCommand.Name));
    }

    [Fact]
    public void Create_WithTooLongDescription_IsInvalid()
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with
            {
                Description = TooLong(Promotion.DescriptionMaxLength + 1)
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreatePromotionCommand.Description));
    }

    [Fact]
    public void Create_WithNullDescription_IsValid()
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with { Description = null });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveDiscountValue_IsInvalid(decimal discountValue)
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with
            {
                DiscountValue = discountValue
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreatePromotionCommand.DiscountValue));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Create_WithPercentageAtOrBelow100_IsValid(int discountValue)
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with
            {
                DiscountType = DiscountType.Percentage,
                DiscountValue = discountValue
            });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    public void Create_WithPercentageAbove100_IsInvalid(int discountValue)
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with
            {
                DiscountType = DiscountType.Percentage,
                DiscountValue = discountValue
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreatePromotionCommand.DiscountValue));
    }

    [Fact]
    public void Create_WithFixedValueAbove100_IsValid()
    {
        // the 100-cap is the percentage rule only; a fixed deal of 500 is shape-legal
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with
            {
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 500
            });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_WithUndefinedDiscountType_IsInvalid()
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with
            {
                DiscountType = (DiscountType)99
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreatePromotionCommand.DiscountType));
    }

    [Fact]
    public void Create_WithoutStartAtUtc_IsInvalid()
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with { StartAtUtc = default });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreatePromotionCommand.StartAtUtc));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithNonPositiveMinOrderAmount_IsInvalid(decimal minOrderAmount)
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with
            {
                MinOrderAmount = minOrderAmount
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreatePromotionCommand.MinOrderAmount));
    }

    [Fact]
    public void Create_WithNonPositiveMaxUsesTotal_IsInvalid()
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with { MaxUsesTotal = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreatePromotionCommand.MaxUsesTotal));
    }

    [Fact]
    public void Create_WithNonPositiveMaxUsesPerCustomer_IsInvalid()
    {
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand() with { MaxUsesPerCustomer = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreatePromotionCommand.MaxUsesPerCustomer));
    }

    [Fact]
    public void Create_WithEndBeforeStart_IsValid()
    {
        // relational rules belong to the domain (DomainException -> 400),
        // not the shape validator - documented by asserting it passes here
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand(
                startAtUtc: PromotionsFixtures.EndsAtUtc,
                endAtUtc: PromotionsFixtures.StartedAtUtc));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_WithPerCustomerAboveTotal_IsValid()
    {
        // same split: which limit sits above which is business logic
        var result = new CreatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidCreateCommand(
                maxUsesTotal: 5, maxUsesPerCustomer: 10));

        Assert.True(result.IsValid);
    }

    // ---------- UpdatePromotion ----------

    [Fact]
    public void Update_WithValidInput_IsValid()
    {
        var result = new UpdatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidUpdateCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Update_WithEmptyPromotionId_IsInvalid()
    {
        var result = new UpdatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidUpdateCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(UpdatePromotionCommand.PromotionId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithEmptyName_IsInvalid(string name)
    {
        var result = new UpdatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidUpdateCommand(Guid.NewGuid()) with
            {
                Name = name
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(UpdatePromotionCommand.Name));
    }

    [Fact]
    public void Update_WithTooLongName_IsInvalid()
    {
        var result = new UpdatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidUpdateCommand(Guid.NewGuid()) with
            {
                Name = TooLong(Promotion.NameMaxLength + 1)
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(UpdatePromotionCommand.Name));
    }

    [Fact]
    public void Update_WithTooLongDescription_IsInvalid()
    {
        var result = new UpdatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidUpdateCommand(Guid.NewGuid()) with
            {
                Description = TooLong(Promotion.DescriptionMaxLength + 1)
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(UpdatePromotionCommand.Description));
    }

    [Fact]
    public void Update_WithoutStartAtUtc_IsInvalid()
    {
        var result = new UpdatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidUpdateCommand(Guid.NewGuid()) with
            {
                StartAtUtc = default
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(UpdatePromotionCommand.StartAtUtc));
    }

    [Fact]
    public void Update_WithNonPositiveMinOrderAmount_IsInvalid()
    {
        var result = new UpdatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidUpdateCommand(Guid.NewGuid()) with
            {
                MinOrderAmount = 0
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(UpdatePromotionCommand.MinOrderAmount));
    }

    [Fact]
    public void Update_WithNonPositiveMaxUsesTotal_IsInvalid()
    {
        var result = new UpdatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidUpdateCommand(Guid.NewGuid()) with
            {
                MaxUsesTotal = -1
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(UpdatePromotionCommand.MaxUsesTotal));
    }

    [Fact]
    public void Update_WithNonPositiveMaxUsesPerCustomer_IsInvalid()
    {
        var result = new UpdatePromotionCommandValidator()
            .Validate(PromotionsFixtures.ValidUpdateCommand(Guid.NewGuid()) with
            {
                MaxUsesPerCustomer = 0
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(UpdatePromotionCommand.MaxUsesPerCustomer));
    }

    // ---------- lifecycle commands ----------

    [Fact]
    public void Activate_WithValidInput_IsValid()
    {
        var result = new ActivatePromotionCommandValidator()
            .Validate(new ActivatePromotionCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Activate_WithEmptyPromotionId_IsInvalid()
    {
        var result = new ActivatePromotionCommandValidator()
            .Validate(new ActivatePromotionCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(ActivatePromotionCommand.PromotionId));
    }

    [Fact]
    public void Pause_WithValidInput_IsValid()
    {
        var result = new PausePromotionCommandValidator()
            .Validate(new PausePromotionCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Pause_WithEmptyPromotionId_IsInvalid()
    {
        var result = new PausePromotionCommandValidator()
            .Validate(new PausePromotionCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(PausePromotionCommand.PromotionId));
    }

    [Fact]
    public void Expire_WithValidInput_IsValid()
    {
        var result = new ExpirePromotionCommandValidator()
            .Validate(new ExpirePromotionCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Expire_WithEmptyPromotionId_IsInvalid()
    {
        var result = new ExpirePromotionCommandValidator()
            .Validate(new ExpirePromotionCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(ExpirePromotionCommand.PromotionId));
    }

    // ---------- RedeemPromotion ----------

    [Fact]
    public void Redeem_WithValidInput_IsValid()
    {
        var result = new RedeemPromotionCommandValidator()
            .Validate(new RedeemPromotionCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Redeem_WithEmptyPromotionId_IsInvalid()
    {
        var result = new RedeemPromotionCommandValidator()
            .Validate(new RedeemPromotionCommand(
                Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 100m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(RedeemPromotionCommand.PromotionId));
    }

    [Fact]
    public void Redeem_WithEmptyCustomerId_IsInvalid()
    {
        var result = new RedeemPromotionCommandValidator()
            .Validate(new RedeemPromotionCommand(
                Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 100m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(RedeemPromotionCommand.CustomerId));
    }

    [Fact]
    public void Redeem_WithEmptyOrderId_IsInvalid()
    {
        var result = new RedeemPromotionCommandValidator()
            .Validate(new RedeemPromotionCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 100m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(RedeemPromotionCommand.OrderId));
    }

    [Fact]
    public void Redeem_WithNegativeOrderAmount_IsInvalid()
    {
        var result = new RedeemPromotionCommandValidator()
            .Validate(new RedeemPromotionCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -0.01m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(RedeemPromotionCommand.OrderAmount));
    }

    [Fact]
    public void Redeem_WithZeroOrderAmount_IsValid()
    {
        // shape allows 0; whether the campaign accepts that order at all is
        // the domain's call (minimum order amount, eligibility)
        var result = new RedeemPromotionCommandValidator()
            .Validate(new RedeemPromotionCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m));

        Assert.True(result.IsValid);
    }

    // ---------- GetPromotions ----------

    [Fact]
    public void GetPromotions_WithDefaults_IsValid()
    {
        var result = new GetPromotionsQueryValidator().Validate(new GetPromotionsQuery());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1001, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void GetPromotions_WithPagingOutOfRange_IsInvalid(int pageNumber, int pageSize)
    {
        var result = new GetPromotionsQueryValidator()
            .Validate(new GetPromotionsQuery(PageNumber: pageNumber, PageSize: pageSize));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(GetPromotionsQuery.PageNumber)
            || e.PropertyName == nameof(GetPromotionsQuery.PageSize));
    }

    [Fact]
    public void GetPromotions_WithTooLongCode_IsInvalid()
    {
        var result = new GetPromotionsQueryValidator()
            .Validate(new GetPromotionsQuery(
                Code: TooLong(Promotion.CodeMaxLength + 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(GetPromotionsQuery.Code));
    }

    [Fact]
    public void GetPromotions_WithNullCode_IsValid()
    {
        // null/empty filter simply means "off"
        var result = new GetPromotionsQueryValidator()
            .Validate(new GetPromotionsQuery(Code: null));

        Assert.True(result.IsValid);
    }
}
