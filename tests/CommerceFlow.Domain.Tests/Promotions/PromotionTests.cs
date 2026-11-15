using CommerceFlow.Domain.Entities.Promotions;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Promotions;

public class PromotionTests
{
    // Fixed instants so every window assertion is deterministic - no test
    // depends on the wall clock (time-sensitive methods take `now`).
    private static readonly DateTimeOffset Start = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2026, 11, 30, 22, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset MidWindow = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid OtherCustomerId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();

    private static Promotion NewPromotion(
        decimal? minOrderAmount = null,
        int? maxUsesTotal = null,
        int? maxUsesPerCustomer = null,
        decimal discountValue = 10m,
        DiscountType discountType = DiscountType.Percentage,
        bool openEnded = false) =>
        Promotion.Create(
            code: "  SAVE10  ",
            name: "Autumn sale",
            discountType: discountType,
            discountValue: discountValue,
            startAtUtc: Start,
            endAtUtc: openEnded ? null : End,
            minOrderAmount: minOrderAmount,
            maxUsesTotal: maxUsesTotal,
            maxUsesPerCustomer: maxUsesPerCustomer);

    private static Promotion ActivePromotion(
        decimal? minOrderAmount = null,
        int? maxUsesTotal = null,
        int? maxUsesPerCustomer = null) =>
        NewPromotion(minOrderAmount, maxUsesTotal, maxUsesPerCustomer)
            .Also(p => p.Activate(now: MidWindow));

    private static Promotion PausedPromotion()
    {
        var promotion = ActivePromotion();
        promotion.Pause();
        return promotion;
    }

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_CreatesDraftPromotionWithWindow()
    {
        var promotion = NewPromotion();

        Assert.Equal("SAVE10", promotion.Code); // trimmed on the way in
        Assert.Equal("Autumn sale", promotion.Name);
        Assert.Null(promotion.Description);
        Assert.Equal(DiscountType.Percentage, promotion.DiscountType);
        Assert.Equal(10m, promotion.DiscountValue);
        Assert.Equal(PromotionStatus.Draft, promotion.Status);
        Assert.Equal(Start, promotion.StartAtUtc);
        Assert.Equal(End, promotion.EndAtUtc);
        Assert.Null(promotion.MinOrderAmount);
        Assert.Null(promotion.MaxUsesTotal);
        Assert.Null(promotion.MaxUsesPerCustomer);
        Assert.Equal(0, promotion.CurrentUses);
        Assert.Empty(promotion.Usages);
        Assert.Null(promotion.ActivatedAtUtc);
        Assert.Null(promotion.PausedAtUtc);
        Assert.Null(promotion.ExpiredAtUtc);
        Assert.NotEqual(Guid.Empty, promotion.Id);
    }

    [Fact]
    public void Create_SetsAuditTimestamps()
    {
        var promotion = NewPromotion();

        Assert.NotEqual(default, promotion.CreatedAtUtc);
        Assert.NotEqual(default, promotion.UpdatedAtUtc);
        Assert.True(promotion.UpdatedAtUtc >= promotion.CreatedAtUtc);
    }

    [Fact]
    public void Create_EmptyCode_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Promotion.Create("  ", "Autumn sale", DiscountType.Percentage, 10, Start));

        Assert.Equal("Promotion code is required.", exception.Message);
    }

    [Fact]
    public void Create_CodeTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Promotion.Create(new string('X', 51), "Autumn sale", DiscountType.Percentage, 10, Start));

        Assert.Equal("Promotion code cannot exceed 50 characters.", exception.Message);
    }

    [Fact]
    public void Create_MaxLengthCode_IsAllowed()
    {
        var promotion = Promotion.Create(
            new string('X', 50), "Autumn sale", DiscountType.Percentage, 10, Start);

        Assert.Equal(50, promotion.Code.Length);
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Promotion.Create("SAVE10", " ", DiscountType.Percentage, 10, Start));

        Assert.Equal("Promotion name is required.", exception.Message);
    }

    [Fact]
    public void Create_NameTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Promotion.Create("SAVE10", new string('N', 151), DiscountType.Percentage, 10, Start));

        Assert.Equal("Promotion name cannot exceed 150 characters.", exception.Message);
    }

    [Fact]
    public void Create_DescriptionTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Promotion.Create("SAVE10", "Autumn sale", DiscountType.Percentage, 10, Start,
                description: new string('D', 501)));

        Assert.Equal("Description cannot exceed 500 characters.", exception.Message);
    }

    [Fact]
    public void Create_WhitespaceDescription_BecomesNull()
    {
        var promotion = Promotion.Create("SAVE10", "Autumn sale", DiscountType.Percentage, 10, Start,
            description: "   ");

        Assert.Null(promotion.Description);
    }

    [Fact]
    public void Create_UnknownDiscountType_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Promotion.Create("SAVE10", "Autumn sale", (DiscountType)99, 10, Start));

        Assert.Equal("Discount type is required.", exception.Message);
    }

    [Fact]
    public void Create_ZeroDiscountValue_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Promotion.Create("SAVE10", "Autumn sale", DiscountType.Percentage, 0, Start));

        Assert.Equal("Discount value must be positive.", exception.Message);
    }

    [Fact]
    public void Create_NegativeDiscountValue_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Promotion.Create("SAVE10", "Autumn sale", DiscountType.FixedAmount, -5, Start));

        Assert.Equal("Discount value must be positive.", exception.Message);
    }

    [Fact]
    public void Create_PercentageOver100_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Promotion.Create("SAVE10", "Autumn sale", DiscountType.Percentage, 100.01m, Start));

        Assert.Equal("Percentage discount cannot exceed 100%.", exception.Message);
    }

    [Fact]
    public void Create_PercentageExactly100_IsAllowed()
    {
        var promotion = Promotion.Create("FREE", "Everything free", DiscountType.Percentage, 100, Start);

        Assert.Equal(100m, promotion.DiscountValue);
    }

    [Fact]
    public void Create_FixedAmountWithoutCap_IsAllowed()
    {
        var promotion = Promotion.Create("SAVE5000", "Big fixed cut", DiscountType.FixedAmount, 5000, Start);

        Assert.Equal(5000m, promotion.DiscountValue);
    }

    [Fact]
    public void Create_DefaultStart_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Promotion.Create("SAVE10", "Autumn sale", DiscountType.Percentage, 10, default));

        Assert.Equal("Start date is required.", exception.Message);
    }

    [Fact]
    public void Create_EndEqualsStart_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Promotion.Create("SAVE10", "Autumn sale", DiscountType.Percentage, 10, Start, endAtUtc: Start));

        Assert.Equal("End date must be after the start date.", exception.Message);
    }

    [Fact]
    public void Create_EndBeforeStart_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Promotion.Create("SAVE10", "Autumn sale", DiscountType.Percentage, 10, Start,
                endAtUtc: Start.AddDays(-1)));

        Assert.Equal("End date must be after the start date.", exception.Message);
    }

    [Fact]
    public void Create_OpenEnded_IsAllowed()
    {
        var promotion = NewPromotion(openEnded: true);

        Assert.Null(promotion.EndAtUtc);
    }

    [Fact]
    public void Create_ZeroMinOrderAmount_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewPromotion(minOrderAmount: 0));

        Assert.Equal("Minimum order amount must be positive.", exception.Message);
    }

    [Fact]
    public void Create_NegativeMinOrderAmount_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewPromotion(minOrderAmount: -1));

        Assert.Equal("Minimum order amount must be positive.", exception.Message);
    }

    [Fact]
    public void Create_ZeroMaxUsesTotal_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewPromotion(maxUsesTotal: 0));

        Assert.Equal("Maximum uses must be positive.", exception.Message);
    }

    [Fact]
    public void Create_ZeroMaxUsesPerCustomer_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewPromotion(maxUsesPerCustomer: 0));

        Assert.Equal("Per-customer usage limit must be positive.", exception.Message);
    }

    [Fact]
    public void Create_PerCustomerAboveTotal_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewPromotion(maxUsesTotal: 5, maxUsesPerCustomer: 6));

        Assert.Equal("Per-customer limit cannot exceed the total limit.", exception.Message);
    }

    [Fact]
    public void Create_PerCustomerEqualsTotal_IsAllowed()
    {
        var promotion = NewPromotion(maxUsesTotal: 5, maxUsesPerCustomer: 5);

        Assert.Equal(5, promotion.MaxUsesTotal);
        Assert.Equal(5, promotion.MaxUsesPerCustomer);
    }

    // ---------- Activate ----------

    [Fact]
    public void Activate_Draft_ActivatesAndStamps()
    {
        var promotion = NewPromotion();

        promotion.Activate(now: MidWindow);

        Assert.Equal(PromotionStatus.Active, promotion.Status);
        Assert.NotNull(promotion.ActivatedAtUtc);
    }

    [Fact]
    public void Activate_Twice_IsNoOpEvenAfterWindowEnd()
    {
        var promotion = ActivePromotion();
        var firstStamp = promotion.ActivatedAtUtc;

        promotion.Activate(now: End.AddDays(1)); // duplicate wins over the calendar

        Assert.Equal(PromotionStatus.Active, promotion.Status);
        Assert.Equal(firstStamp, promotion.ActivatedAtUtc);
    }

    [Fact]
    public void Activate_Paused_ResumesWithoutRestampingFirstPublish()
    {
        var promotion = PausedPromotion();
        var firstStamp = promotion.ActivatedAtUtc;

        promotion.Activate(now: MidWindow);

        Assert.Equal(PromotionStatus.Active, promotion.Status);
        Assert.Equal(firstStamp, promotion.ActivatedAtUtc); // first publish survives a resume
    }

    [Fact]
    public void Activate_Expired_Throws()
    {
        var promotion = NewPromotion();
        promotion.Expire();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.Activate(now: MidWindow));

        Assert.Equal("Promotion cannot move from Expired to Active.", exception.Message);
    }

    [Fact]
    public void Activate_WindowAlreadyEnded_Throws()
    {
        var promotion = NewPromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.Activate(now: End.AddDays(1)));

        Assert.Equal("Promotion window has already ended.", exception.Message);
        Assert.Equal(PromotionStatus.Draft, promotion.Status); // nothing moved
    }

    [Fact]
    public void Activate_FutureStart_IsAllowed()
    {
        var promotion = Promotion.Create(
            "SAVE10", "Autumn sale", DiscountType.Percentage, 10,
            startAtUtc: Start.AddMonths(1), endAtUtc: Start.AddMonths(2));

        promotion.Activate(now: Start.AddDays(-1)); // scheduled: published early

        Assert.Equal(PromotionStatus.Active, promotion.Status);
    }

    [Fact]
    public void Activate_ExpiredWithEndedWindow_ReportsTransitionFirst()
    {
        // State check runs before the clock check (same order as
        // ReturnRequest.MarkReceived): the terminal state is the real problem.
        var promotion = NewPromotion();
        promotion.Expire();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.Activate(now: End.AddDays(1)));

        Assert.Equal("Promotion cannot move from Expired to Active.", exception.Message);
    }

    // ---------- Pause ----------

    [Fact]
    public void Pause_Active_PausesAndStamps()
    {
        var promotion = ActivePromotion();

        promotion.Pause();

        Assert.Equal(PromotionStatus.Paused, promotion.Status);
        Assert.NotNull(promotion.PausedAtUtc);
    }

    [Fact]
    public void Pause_Twice_IsNoOp()
    {
        var promotion = PausedPromotion();
        var stamp = promotion.PausedAtUtc;

        promotion.Pause();

        Assert.Equal(PromotionStatus.Paused, promotion.Status);
        Assert.Equal(stamp, promotion.PausedAtUtc);
    }

    [Fact]
    public void Pause_Draft_Throws()
    {
        var promotion = NewPromotion();

        var exception = Assert.Throws<DomainException>(() => promotion.Pause());

        Assert.Equal("Promotion cannot move from Draft to Paused.", exception.Message);
    }

    [Fact]
    public void Pause_Expired_Throws()
    {
        var promotion = NewPromotion();
        promotion.Expire();

        var exception = Assert.Throws<DomainException>(() => promotion.Pause());

        Assert.Equal("Promotion cannot move from Expired to Paused.", exception.Message);
    }

    // ---------- Expire ----------

    [Fact]
    public void Expire_Active_ExpiresAndStamps()
    {
        var promotion = ActivePromotion();

        promotion.Expire();

        Assert.Equal(PromotionStatus.Expired, promotion.Status);
        Assert.NotNull(promotion.ExpiredAtUtc);
    }

    [Fact]
    public void Expire_Draft_IsAllowed()
    {
        var promotion = NewPromotion();

        promotion.Expire();

        Assert.Equal(PromotionStatus.Expired, promotion.Status);
    }

    [Fact]
    public void Expire_Paused_IsAllowed()
    {
        var promotion = PausedPromotion();

        promotion.Expire();

        Assert.Equal(PromotionStatus.Expired, promotion.Status);
    }

    [Fact]
    public void Expire_AlreadyExpired_IsNoOp()
    {
        var promotion = NewPromotion();
        promotion.Expire();
        var stamp = promotion.ExpiredAtUtc;

        promotion.Expire();

        Assert.Equal(PromotionStatus.Expired, promotion.Status);
        Assert.Equal(stamp, promotion.ExpiredAtUtc);
    }

    // ---------- ExpireIfDue ----------

    [Fact]
    public void ExpireIfDue_PastEnd_ActiveExpires()
    {
        var promotion = ActivePromotion();

        promotion.ExpireIfDue(now: End);

        Assert.Equal(PromotionStatus.Expired, promotion.Status);
    }

    [Fact]
    public void ExpireIfDue_PastEnd_PausedExpires()
    {
        var promotion = PausedPromotion();

        promotion.ExpireIfDue(now: End.AddDays(5));

        Assert.Equal(PromotionStatus.Expired, promotion.Status);
    }

    [Fact]
    public void ExpireIfDue_BeforeEnd_StaysActive()
    {
        var promotion = ActivePromotion();

        promotion.ExpireIfDue(now: End.AddTicks(-1));

        Assert.Equal(PromotionStatus.Active, promotion.Status);
    }

    [Fact]
    public void ExpireIfDue_NoEnd_NeverExpires()
    {
        var promotion = NewPromotion(openEnded: true);
        promotion.Activate(now: MidWindow);

        promotion.ExpireIfDue(now: Start.AddYears(10));

        Assert.Equal(PromotionStatus.Active, promotion.Status);
    }

    [Fact]
    public void ExpireIfDue_Draft_StaysDraft()
    {
        // Not yet published: the clock does not get to end what never started
        // (Activate will refuse it later with the window message instead).
        var promotion = NewPromotion();

        promotion.ExpireIfDue(now: End.AddDays(1));

        Assert.Equal(PromotionStatus.Draft, promotion.Status);
    }

    [Fact]
    public void ExpireIfDue_AlreadyExpired_StaysExpired()
    {
        var promotion = ActivePromotion();
        promotion.Expire();
        var stamp = promotion.ExpiredAtUtc;

        promotion.ExpireIfDue(now: End.AddDays(1));

        Assert.Equal(PromotionStatus.Expired, promotion.Status);
        Assert.Equal(stamp, promotion.ExpiredAtUtc);
    }

    // ---------- UpdateDetails ----------

    [Fact]
    public void UpdateDetails_Draft_UpdatesEditableFields()
    {
        var promotion = NewPromotion();

        promotion.UpdateDetails(
            name: "Winter sale",
            description: "  Frosty deals  ",
            startAtUtc: Start.AddDays(10),
            endAtUtc: End.AddDays(10),
            minOrderAmount: 75m,
            maxUsesTotal: 200,
            maxUsesPerCustomer: 3);

        Assert.Equal("Winter sale", promotion.Name);
        Assert.Equal("Frosty deals", promotion.Description);
        Assert.Equal(Start.AddDays(10), promotion.StartAtUtc);
        Assert.Equal(End.AddDays(10), promotion.EndAtUtc);
        Assert.Equal(75m, promotion.MinOrderAmount);
        Assert.Equal(200, promotion.MaxUsesTotal);
        Assert.Equal(3, promotion.MaxUsesPerCustomer);
    }

    [Fact]
    public void UpdateDetails_Paused_IsAllowed()
    {
        var promotion = PausedPromotion();

        promotion.UpdateDetails("Winter sale", null, Start, End, null, null, null);

        Assert.Equal("Winter sale", promotion.Name);
    }

    [Fact]
    public void UpdateDetails_Active_Throws()
    {
        var promotion = ActivePromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.UpdateDetails("Winter sale", null, Start, End, null, null, null));

        Assert.Equal(
            "Promotion details can only be updated while the promotion is draft or paused.",
            exception.Message);
    }

    [Fact]
    public void UpdateDetails_Expired_Throws()
    {
        var promotion = NewPromotion();
        promotion.Expire();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.UpdateDetails("Winter sale", null, Start, End, null, null, null));

        Assert.Equal(
            "Promotion details can only be updated while the promotion is draft or paused.",
            exception.Message);
    }

    [Fact]
    public void UpdateDetails_EmptyName_Throws()
    {
        var promotion = NewPromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.UpdateDetails(" ", null, Start, End, null, null, null));

        Assert.Equal("Promotion name is required.", exception.Message);
    }

    [Fact]
    public void UpdateDetails_EndBeforeStart_Throws()
    {
        var promotion = NewPromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.UpdateDetails("Autumn sale", null, Start, Start.AddDays(-1), null, null, null));

        Assert.Equal("End date must be after the start date.", exception.Message);
    }

    [Fact]
    public void UpdateDetails_PerCustomerAboveTotal_Throws()
    {
        var promotion = NewPromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.UpdateDetails("Autumn sale", null, Start, End, null,
                maxUsesTotal: 5, maxUsesPerCustomer: 6));

        Assert.Equal("Per-customer limit cannot exceed the total limit.", exception.Message);
    }

    [Fact]
    public void UpdateDetails_TotalBelowUsesRecorded_Throws()
    {
        var promotion = ActivePromotion(maxUsesTotal: 5);
        promotion.RecordUsage(CustomerId, OrderId, 10m, now: MidWindow);
        promotion.RecordUsage(OtherCustomerId, Guid.NewGuid(), 10m, now: MidWindow);
        promotion.Pause();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.UpdateDetails("Autumn sale", null, Start, End, null,
                maxUsesTotal: 1, maxUsesPerCustomer: null));

        Assert.Equal("Total limit cannot be lower than the uses already recorded.",
            exception.Message);
    }

    [Fact]
    public void UpdateDetails_NeverTouchesCodeOrDiscount()
    {
        var promotion = NewPromotion(discountValue: 25m, discountType: DiscountType.FixedAmount);

        promotion.UpdateDetails("Winter sale", null, Start, End, null, null, null);

        Assert.Equal("SAVE10", promotion.Code);
        Assert.Equal(DiscountType.FixedAmount, promotion.DiscountType);
        Assert.Equal(25m, promotion.DiscountValue);
    }

    // ---------- EnsureCanRedeem ----------

    [Fact]
    public void EnsureCanRedeem_ActiveWithinWindowNoLimits_DoesNotThrow()
    {
        var promotion = ActivePromotion();

        promotion.EnsureCanRedeem(CustomerId, orderAmount: 100m, now: MidWindow);
    }

    [Fact]
    public void EnsureCanRedeem_EmptyCustomerId_Throws()
    {
        var promotion = ActivePromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.EnsureCanRedeem(Guid.Empty, 100m, now: MidWindow));

        Assert.Equal("Customer ID is required.", exception.Message);
    }

    [Fact]
    public void EnsureCanRedeem_NegativeOrderAmount_Throws()
    {
        var promotion = ActivePromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.EnsureCanRedeem(CustomerId, -1m, now: MidWindow));

        Assert.Equal("Order amount cannot be negative.", exception.Message);
    }

    [Fact]
    public void EnsureCanRedeem_Draft_Throws()
    {
        var promotion = NewPromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.EnsureCanRedeem(CustomerId, 100m, now: MidWindow));

        Assert.Equal("Promotion cannot be redeemed while its status is Draft.",
            exception.Message);
    }

    [Fact]
    public void EnsureCanRedeem_Paused_Throws()
    {
        var promotion = PausedPromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.EnsureCanRedeem(CustomerId, 100m, now: MidWindow));

        Assert.Equal("Promotion cannot be redeemed while its status is Paused.",
            exception.Message);
    }

    [Fact]
    public void EnsureCanRedeem_Expired_Throws()
    {
        var promotion = ActivePromotion();
        promotion.Expire();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.EnsureCanRedeem(CustomerId, 100m, now: MidWindow));

        Assert.Equal("Promotion cannot be redeemed while its status is Expired.",
            exception.Message);
    }

    [Fact]
    public void EnsureCanRedeem_BeforeStart_Throws()
    {
        var promotion = ActivePromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.EnsureCanRedeem(CustomerId, 100m, now: Start.AddTicks(-1)));

        Assert.Equal("Promotion has not started yet.", exception.Message);
    }

    [Fact]
    public void EnsureCanRedeem_AtStart_IsAllowed()
    {
        // Window is [start, end): opening tick counts.
        var promotion = ActivePromotion();

        promotion.EnsureCanRedeem(CustomerId, 100m, now: Start);
    }

    [Fact]
    public void EnsureCanRedeem_AtEnd_Throws()
    {
        // ...and the closing tick does not (end is exclusive).
        var promotion = ActivePromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.EnsureCanRedeem(CustomerId, 100m, now: End));

        Assert.Equal("Promotion has ended.", exception.Message);
    }

    [Fact]
    public void EnsureCanRedeem_AfterEnd_Throws()
    {
        var promotion = ActivePromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.EnsureCanRedeem(CustomerId, 100m, now: End.AddDays(1)));

        Assert.Equal("Promotion has ended.", exception.Message);
    }

    [Fact]
    public void EnsureCanRedeem_TotalLimitReached_Throws()
    {
        var promotion = ActivePromotion(maxUsesTotal: 1);
        promotion.RecordUsage(CustomerId, OrderId, 10m, now: MidWindow);

        var exception = Assert.Throws<DomainException>(() =>
            promotion.EnsureCanRedeem(OtherCustomerId, 100m, now: MidWindow));

        Assert.Equal("Promotion has reached its usage limit.", exception.Message);
    }

    [Fact]
    public void EnsureCanRedeem_TotalLimitNotReached_IsAllowed()
    {
        var promotion = ActivePromotion(maxUsesTotal: 2);
        promotion.RecordUsage(CustomerId, OrderId, 10m, now: MidWindow);

        promotion.EnsureCanRedeem(OtherCustomerId, 100m, now: MidWindow);
        Assert.Equal(1, promotion.CurrentUses);
    }

    [Fact]
    public void EnsureCanRedeem_PerCustomerLimitReached_Throws()
    {
        var promotion = ActivePromotion(maxUsesPerCustomer: 1);
        promotion.RecordUsage(CustomerId, OrderId, 10m, now: MidWindow);

        var exception = Assert.Throws<DomainException>(() =>
            promotion.EnsureCanRedeem(CustomerId, 100m, now: MidWindow));

        Assert.Equal("Customer has reached the usage limit for this promotion.",
            exception.Message);
    }

    [Fact]
    public void EnsureCanRedeem_PerCustomerLimitReached_OtherCustomerAllowed()
    {
        var promotion = ActivePromotion(maxUsesPerCustomer: 1);
        promotion.RecordUsage(CustomerId, OrderId, 10m, now: MidWindow);

        promotion.EnsureCanRedeem(OtherCustomerId, 100m, now: MidWindow);
    }

    [Fact]
    public void EnsureCanRedeem_BelowMinOrder_Throws()
    {
        var promotion = ActivePromotion(minOrderAmount: 50m);

        var exception = Assert.Throws<DomainException>(() =>
            promotion.EnsureCanRedeem(CustomerId, 49.99m, now: MidWindow));

        Assert.Equal("Order amount does not meet the promotion's minimum order amount.",
            exception.Message);
    }

    [Fact]
    public void EnsureCanRedeem_ExactlyMinOrder_IsAllowed()
    {
        var promotion = ActivePromotion(minOrderAmount: 50m);

        promotion.EnsureCanRedeem(CustomerId, 50m, now: MidWindow);
    }

    // ---------- RecordUsage ----------

    [Fact]
    public void RecordUsage_Valid_RecordsUsageAndIncrements()
    {
        var promotion = ActivePromotion();

        promotion.RecordUsage(CustomerId, OrderId, 12.5m, now: MidWindow);

        Assert.Equal(1, promotion.CurrentUses);
        var usage = Assert.Single(promotion.Usages);
        Assert.Equal(promotion.Id, usage.PromotionId);
        Assert.Equal(CustomerId, usage.CustomerId);
        Assert.Equal(OrderId, usage.OrderId);
        Assert.Equal(12.5m, usage.DiscountTaken);
        Assert.NotEqual(default, usage.UsedAtUtc);
    }

    [Fact]
    public void RecordUsage_EmptyCustomerId_Throws()
    {
        var promotion = ActivePromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.RecordUsage(Guid.Empty, OrderId, 10m, now: MidWindow));

        Assert.Equal("Customer ID is required.", exception.Message);
        Assert.Equal(0, promotion.CurrentUses);
    }

    [Fact]
    public void RecordUsage_EmptyOrderId_Throws()
    {
        var promotion = ActivePromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.RecordUsage(CustomerId, Guid.Empty, 10m, now: MidWindow));

        Assert.Equal("Order ID is required.", exception.Message);
        Assert.Equal(0, promotion.CurrentUses);
    }

    [Fact]
    public void RecordUsage_ZeroDiscount_Throws()
    {
        var promotion = ActivePromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.RecordUsage(CustomerId, OrderId, 0m, now: MidWindow));

        Assert.Equal("Discount taken must be positive.", exception.Message);
        Assert.Empty(promotion.Usages);
    }

    [Fact]
    public void RecordUsage_NegativeDiscount_Throws()
    {
        var promotion = ActivePromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.RecordUsage(CustomerId, OrderId, -1m, now: MidWindow));

        Assert.Equal("Discount taken must be positive.", exception.Message);
    }

    [Fact]
    public void RecordUsage_NotActive_ThrowsAsSafetyNet()
    {
        // Caller skipped EnsureCanRedeem - the domain still refuses.
        var promotion = NewPromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.RecordUsage(CustomerId, OrderId, 10m, now: MidWindow));

        Assert.Equal("Promotion cannot be redeemed while its status is Draft.",
            exception.Message);
        Assert.Empty(promotion.Usages);
    }

    [Fact]
    public void RecordUsage_OutsideWindow_ThrowsAsSafetyNet()
    {
        var promotion = ActivePromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.RecordUsage(CustomerId, OrderId, 10m, now: End.AddDays(1)));

        Assert.Equal("Promotion has ended.", exception.Message);
        Assert.Empty(promotion.Usages);
    }

    [Fact]
    public void RecordUsage_TotalLimitReached_Throws()
    {
        var promotion = ActivePromotion(maxUsesTotal: 1);
        promotion.RecordUsage(CustomerId, OrderId, 10m, now: MidWindow);

        var exception = Assert.Throws<DomainException>(() =>
            promotion.RecordUsage(OtherCustomerId, Guid.NewGuid(), 10m, now: MidWindow));

        Assert.Equal("Promotion has reached its usage limit.", exception.Message);
        Assert.Equal(1, promotion.CurrentUses);
    }

    [Fact]
    public void RecordUsage_PerCustomerLimitReached_Throws()
    {
        var promotion = ActivePromotion(maxUsesPerCustomer: 1);
        promotion.RecordUsage(CustomerId, OrderId, 10m, now: MidWindow);

        var exception = Assert.Throws<DomainException>(() =>
            promotion.RecordUsage(CustomerId, Guid.NewGuid(), 10m, now: MidWindow));

        Assert.Equal("Customer has reached the usage limit for this promotion.",
            exception.Message);
        Assert.Equal(1, promotion.CurrentUses);
    }

    // ---------- CalculateDiscount ----------

    [Fact]
    public void CalculateDiscount_Percentage_RoundsHalfAwayFromZero()
    {
        var promotion = NewPromotion(discountValue: 10m);

        // 10% of 99.99 = 9.999 -> 10.00
        Assert.Equal(10.00m, promotion.CalculateDiscount(99.99m));
    }

    [Fact]
    public void CalculateDiscount_Percentage_ExactResult()
    {
        var promotion = NewPromotion(discountValue: 15m);

        Assert.Equal(15.00m, promotion.CalculateDiscount(100m));
    }

    [Fact]
    public void CalculateDiscount_Percentage_RoundsToTwoDecimals()
    {
        var promotion = NewPromotion(discountValue: 33.33m);

        // 33.33% of 10 = 3.333 -> 3.33
        Assert.Equal(3.33m, promotion.CalculateDiscount(10m));
    }

    [Fact]
    public void CalculateDiscount_Percentage100_ReturnsFullAmount()
    {
        var promotion = Promotion.Create("FREE", "Everything free",
            DiscountType.Percentage, 100, Start);

        Assert.Equal(50m, promotion.CalculateDiscount(50m));
    }

    [Fact]
    public void CalculateDiscount_Fixed_ReturnsValue()
    {
        var promotion = NewPromotion(discountValue: 25m, discountType: DiscountType.FixedAmount);

        Assert.Equal(25m, promotion.CalculateDiscount(100m));
    }

    [Fact]
    public void CalculateDiscount_Fixed_CapsAtOrderAmount()
    {
        var promotion = NewPromotion(discountValue: 25m, discountType: DiscountType.FixedAmount);

        Assert.Equal(10m, promotion.CalculateDiscount(10m));
    }

    [Fact]
    public void CalculateDiscount_ZeroOrderAmount_ReturnsZero()
    {
        var promotion = NewPromotion(discountValue: 25m, discountType: DiscountType.FixedAmount);

        Assert.Equal(0m, promotion.CalculateDiscount(0m));
    }

    [Fact]
    public void CalculateDiscount_NegativeOrderAmount_Throws()
    {
        var promotion = NewPromotion();

        var exception = Assert.Throws<DomainException>(() =>
            promotion.CalculateDiscount(-1m));

        Assert.Equal("Order amount cannot be negative.", exception.Message);
    }
}

// Small helper so setup reads as a single intention (used by the state tests).
internal static class PromotionTestExtensions
{
    public static T Also<T>(this T value, Action<T> act)
    {
        act(value);
        return value;
    }
}
