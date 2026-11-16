using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Promotions.Commands.ActivatePromotion;
using CommerceFlow.Application.Promotions.Commands.CreatePromotion;
using CommerceFlow.Application.Promotions.Commands.ExpirePromotion;
using CommerceFlow.Application.Promotions.Commands.PausePromotion;
using CommerceFlow.Application.Promotions.Commands.RedeemPromotion;
using CommerceFlow.Application.Promotions.Commands.UpdatePromotion;
using CommerceFlow.Application.Promotions.Interfaces;
using CommerceFlow.Domain.Entities.Promotions;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;
using Moq;

namespace CommerceFlow.Application.Tests.Promotions;

public sealed class PromotionsCommandHandlerTests
{
    private readonly Mock<IPromotionRepository> _promotions = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void SetupPromotion(Promotion promotion) =>
        _promotions
            .Setup(x => x.GetByIdAsync(promotion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

    private void SetupPromotionMissing(Guid id) =>
        _promotions
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

    // every create test that passes the code pre-check needs this: the
    // uniqueness check reads the repository, and loose Moq would hand null
    private void SetupNoExistingCode() =>
        _promotions
            .Setup(x => x.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

    private void SetupExistingCode(Promotion taken) =>
        _promotions
            .Setup(x => x.GetByCodeAsync(taken.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taken);

    private static void VerifyNeverSaved(Mock<IUnitOfWork> unitOfWork) =>
        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    // ---------- CreatePromotion ----------

    [Fact]
    public async Task Create_WithValidInput_CreatesDraftAndSaves()
    {
        var handler = new CreatePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        SetupNoExistingCode();

        var id = await handler.Handle(
            PromotionsFixtures.ValidCreateCommand(code: "  SAVE10  "),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _promotions.Verify(
            x => x.GetByCodeAsync("SAVE10", It.IsAny<CancellationToken>()), // trimmed
            Times.Once);
        _promotions.Verify(
            x => x.AddAsync(
                It.Is<Promotion>(p => p.Id == id
                    && p.Code == "SAVE10" // trimmed by the domain too
                    && p.Name == "Autumn sale"
                    && p.DiscountType == DiscountType.Percentage
                    && p.DiscountValue == 10
                    && p.Status == PromotionStatus.Draft // Create never publishes
                    && p.CurrentUses == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenCodeAlreadyExists_ThrowsConflict()
    {
        var handler = new CreatePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        SetupExistingCode(PromotionsFixtures.CreatePromotion(code: "SAVE10"));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                PromotionsFixtures.ValidCreateCommand(), CancellationToken.None));

        Assert.Contains("A promotion with code 'SAVE10' already exists.", exception.Message);
        _promotions.Verify(
            x => x.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Create_WhenEndIsBeforeStart_ThrowsDomainException()
    {
        var handler = new CreatePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        SetupNoExistingCode();

        // relational rules are the domain's; the exception bubbles as-is
        // (mapped to 400, never swallowed)
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                PromotionsFixtures.ValidCreateCommand(
                    startAtUtc: PromotionsFixtures.EndsAtUtc,
                    endAtUtc: PromotionsFixtures.StartedAtUtc),
                CancellationToken.None));

        _promotions.Verify(
            x => x.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- UpdatePromotion ----------

    [Fact]
    public async Task Update_OnDraft_AppliesDetailsAndSaves()
    {
        var handler = new UpdatePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(); // Draft
        SetupPromotion(promotion);

        await handler.Handle(
            PromotionsFixtures.ValidUpdateCommand(
                promotion.Id, description: null, minOrderAmount: 25, maxUsesTotal: 100),
            CancellationToken.None);

        Assert.Equal("Autumn sale v2", promotion.Name);
        Assert.Null(promotion.Description); // null clears the text
        Assert.Equal(25, promotion.MinOrderAmount);
        Assert.Equal(100, promotion.MaxUsesTotal);
        Assert.Equal("SAVE10", promotion.Code); // frozen after create
        Assert.Equal(10, promotion.DiscountValue); // frozen after create
        Assert.Equal(PromotionStatus.Draft, promotion.Status); // editing never publishes
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_OnPaused_AppliesTextAndSaves()
    {
        var handler = new UpdatePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(
            status: PromotionStatus.Paused);
        SetupPromotion(promotion);

        await handler.Handle(
            PromotionsFixtures.ValidUpdateCommand(promotion.Id),
            CancellationToken.None);

        Assert.Equal("Autumn sale v2", promotion.Name);
        Assert.Equal("Ten percent off, revised.", promotion.Description);
        Assert.Equal(PromotionStatus.Paused, promotion.Status); // state untouched
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WhenPromotionDoesNotExist_ThrowsNotFound()
    {
        var handler = new UpdatePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        SetupPromotionMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                PromotionsFixtures.ValidUpdateCommand(Guid.NewGuid()),
                CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(PromotionStatus.Active)]
    [InlineData(PromotionStatus.Expired)]
    public async Task Update_WhenPromotionIsNotEditable_ThrowsConflict(PromotionStatus status)
    {
        var handler = new UpdatePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(status: status);
        SetupPromotion(promotion);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                PromotionsFixtures.ValidUpdateCommand(promotion.Id, name: "Renamed"),
                CancellationToken.None));

        Assert.Contains(
            $"Only draft or paused promotions can be updated (current: {status}).",
            exception.Message);
        Assert.Equal("Autumn sale", promotion.Name); // untouched
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Update_WhenTotalLimitFallsBelowUses_ThrowsDomainException()
    {
        var handler = new UpdatePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        // a paused campaign may carry redemptions - the ceiling must never
        // be allowed to sink under what was already recorded
        var promotion = PromotionsFixtures.CreatePromotion(
            status: PromotionStatus.Paused, maxUsesTotal: 10, seededUses: 2);
        SetupPromotion(promotion);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                PromotionsFixtures.ValidUpdateCommand(promotion.Id, maxUsesTotal: 1),
                CancellationToken.None));

        Assert.Contains(
            "Total limit cannot be lower than the uses already recorded.",
            exception.Message);
        Assert.Equal("Autumn sale", promotion.Name); // whole update refused
        Assert.Equal(10, promotion.MaxUsesTotal);
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- ActivatePromotion ----------

    [Fact]
    public async Task Activate_FromDraft_PublishesAndSaves()
    {
        var handler = new ActivatePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(); // Draft
        SetupPromotion(promotion);

        await handler.Handle(new ActivatePromotionCommand(promotion.Id), CancellationToken.None);

        Assert.Equal(PromotionStatus.Active, promotion.Status);
        Assert.NotNull(promotion.ActivatedAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activate_FromPaused_ResumesKeepingFirstPublishStamp()
    {
        var handler = new ActivatePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(
            status: PromotionStatus.Paused);
        var firstStamp = promotion.ActivatedAtUtc;
        Assert.NotNull(firstStamp);
        SetupPromotion(promotion);

        await handler.Handle(new ActivatePromotionCommand(promotion.Id), CancellationToken.None);

        Assert.Equal(PromotionStatus.Active, promotion.Status);
        Assert.Equal(firstStamp, promotion.ActivatedAtUtc); // resume, not re-publish
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activate_WhenPromotionDoesNotExist_ThrowsNotFound()
    {
        var handler = new ActivatePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        SetupPromotionMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ActivatePromotionCommand(Guid.NewGuid()), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(PromotionStatus.Active)]
    [InlineData(PromotionStatus.Expired)]
    public async Task Activate_WhenPromotionCannotBePublished_ThrowsConflict(PromotionStatus status)
    {
        var handler = new ActivatePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(status: status);
        SetupPromotion(promotion);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ActivatePromotionCommand(promotion.Id), CancellationToken.None));

        Assert.Contains(
            $"Only draft or paused promotions can be activated (current: {status}).",
            exception.Message);
        Assert.Equal(status, promotion.Status); // untouched
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Activate_WhenWindowHasAlreadyEnded_ThrowsConflict()
    {
        var handler = new ActivatePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(
            endAtUtc: DateTimeOffset.UtcNow.AddDays(-1)); // Draft, window closed
        SetupPromotion(promotion);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ActivatePromotionCommand(promotion.Id), CancellationToken.None));

        Assert.Contains("Promotion window has already ended.", exception.Message);
        Assert.Equal(PromotionStatus.Draft, promotion.Status); // never published
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- PausePromotion ----------

    [Fact]
    public async Task Pause_FromActive_TakesOfflineAndSaves()
    {
        var handler = new PausePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(
            status: PromotionStatus.Active);
        SetupPromotion(promotion);

        await handler.Handle(new PausePromotionCommand(promotion.Id), CancellationToken.None);

        Assert.Equal(PromotionStatus.Paused, promotion.Status);
        Assert.NotNull(promotion.PausedAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pause_WhenPromotionDoesNotExist_ThrowsNotFound()
    {
        var handler = new PausePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        SetupPromotionMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new PausePromotionCommand(Guid.NewGuid()), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(PromotionStatus.Draft)]
    [InlineData(PromotionStatus.Paused)]
    [InlineData(PromotionStatus.Expired)]
    public async Task Pause_WhenPromotionIsNotActive_ThrowsConflict(PromotionStatus status)
    {
        var handler = new PausePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(status: status);
        SetupPromotion(promotion);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new PausePromotionCommand(promotion.Id), CancellationToken.None));

        Assert.Contains(
            $"Only active promotions can be paused (current: {status}).",
            exception.Message);
        Assert.Equal(status, promotion.Status); // untouched
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- ExpirePromotion ----------

    [Theory]
    [InlineData(PromotionStatus.Draft)]
    [InlineData(PromotionStatus.Active)]
    [InlineData(PromotionStatus.Paused)]
    public async Task Expire_FromNonTerminalState_EndsAndSaves(PromotionStatus status)
    {
        var handler = new ExpirePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(status: status);
        SetupPromotion(promotion);

        await handler.Handle(new ExpirePromotionCommand(promotion.Id), CancellationToken.None);

        Assert.Equal(PromotionStatus.Expired, promotion.Status);
        Assert.NotNull(promotion.ExpiredAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Expire_WhenPromotionDoesNotExist_ThrowsNotFound()
    {
        var handler = new ExpirePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        SetupPromotionMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ExpirePromotionCommand(Guid.NewGuid()), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Expire_OnExpiredPromotion_IsTerminalConflict()
    {
        var handler = new ExpirePromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(
            status: PromotionStatus.Expired);
        var originalStamp = promotion.ExpiredAtUtc;
        SetupPromotion(promotion);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ExpirePromotionCommand(promotion.Id), CancellationToken.None));

        Assert.Contains(
            "Only draft, active or paused promotions can be expired (current: Expired).",
            exception.Message);
        Assert.Equal(originalStamp, promotion.ExpiredAtUtc); // original kept
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- RedeemPromotion ----------
    // eligibility failures are business conflicts: the handler maps the
    // domain's DomainException to ConflictException (409, never 400)

    [Fact]
    public async Task Redeem_OnEligiblePromotion_TakesDiscountAndSaves()
    {
        var handler = new RedeemPromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(
            status: PromotionStatus.Active);
        SetupPromotion(promotion);
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var result = await handler.Handle(
            new RedeemPromotionCommand(promotion.Id, customerId, orderId, 100m),
            CancellationToken.None);

        Assert.Equal(10m, result.DiscountTaken); // 10% of 100
        Assert.Equal(1, result.CurrentUses);
        Assert.Equal(1, promotion.CurrentUses);
        var usage = Assert.Single(promotion.Usages);
        Assert.Equal(customerId, usage.CustomerId);
        Assert.Equal(orderId, usage.OrderId);
        Assert.Equal(10m, usage.DiscountTaken);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Redeem_WithFixedDiscount_TakesFlatAmount()
    {
        var handler = new RedeemPromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(
            status: PromotionStatus.Active,
            discountType: DiscountType.FixedAmount,
            discountValue: 5);
        SetupPromotion(promotion);

        var result = await handler.Handle(
            new RedeemPromotionCommand(
                promotion.Id, Guid.NewGuid(), Guid.NewGuid(), 100m),
            CancellationToken.None);

        Assert.Equal(5m, result.DiscountTaken); // flat, not a percentage
        Assert.Equal(1, result.CurrentUses);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Redeem_WhenPromotionDoesNotExist_ThrowsNotFound()
    {
        var handler = new RedeemPromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        SetupPromotionMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new RedeemPromotionCommand(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m),
                CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Redeem_OnDraftPromotion_ThrowsConflictWithDomainReason()
    {
        var handler = new RedeemPromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(); // Draft
        SetupPromotion(promotion);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new RedeemPromotionCommand(
                    promotion.Id, Guid.NewGuid(), Guid.NewGuid(), 100m),
                CancellationToken.None));

        Assert.Contains(
            "Promotion cannot be redeemed while its status is Draft.",
            exception.Message);
        Assert.Equal(0, promotion.CurrentUses);
        Assert.Empty(promotion.Usages);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Redeem_WhenTotalUsageLimitReached_ThrowsConflict()
    {
        var handler = new RedeemPromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(
            status: PromotionStatus.Active, maxUsesTotal: 1, seededUses: 1);
        SetupPromotion(promotion);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new RedeemPromotionCommand(
                    promotion.Id, Guid.NewGuid(), Guid.NewGuid(), 100m),
                CancellationToken.None));

        Assert.Contains("Promotion has reached its usage limit.", exception.Message);
        Assert.Equal(1, promotion.CurrentUses); // unchanged
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Redeem_WhenPerCustomerLimitReached_ThrowsConflict()
    {
        var handler = new RedeemPromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(
            status: PromotionStatus.Active,
            maxUsesPerCustomer: 1,
            seededUses: 1); // seeded for PromotionsFixtures.CustomerId
        SetupPromotion(promotion);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new RedeemPromotionCommand(
                    promotion.Id, PromotionsFixtures.CustomerId, Guid.NewGuid(), 100m),
                CancellationToken.None));

        Assert.Contains(
            "Customer has reached the usage limit for this promotion.",
            exception.Message);
        Assert.Equal(1, promotion.CurrentUses); // unchanged
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Redeem_WhenOrderAlreadyRedeemed_ThrowsConflict()
    {
        var handler = new RedeemPromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(
            status: PromotionStatus.Active);
        SetupPromotion(promotion);
        var orderId = Guid.NewGuid();

        await handler.Handle(
            new RedeemPromotionCommand(promotion.Id, Guid.NewGuid(), orderId, 100m),
            CancellationToken.None);

        // another customer, same order: the per-customer path stays clear so
        // the one-redemption-per-order rule is what refuses it
        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new RedeemPromotionCommand(promotion.Id, Guid.NewGuid(), orderId, 100m),
                CancellationToken.None));

        Assert.Contains(
            "Promotion has already been redeemed on this order.",
            exception.Message);
        Assert.Equal(1, promotion.CurrentUses); // second attempt recorded nothing
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once); // first call only
    }

    [Fact]
    public async Task Redeem_WhenOrderIsBelowMinimum_ThrowsConflict()
    {
        var handler = new RedeemPromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(
            status: PromotionStatus.Active, minOrderAmount: 50);
        SetupPromotion(promotion);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new RedeemPromotionCommand(
                    promotion.Id, Guid.NewGuid(), Guid.NewGuid(), 49.99m),
                CancellationToken.None));

        Assert.Contains(
            "Order amount does not meet the promotion's minimum order amount.",
            exception.Message);
        Assert.Equal(0, promotion.CurrentUses);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Redeem_WhenWindowHasNotOpened_ThrowsConflict()
    {
        var handler = new RedeemPromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        var promotion = PromotionsFixtures.CreatePromotion(
            status: PromotionStatus.Active,
            startAtUtc: DateTimeOffset.UtcNow.AddDays(7)); // scheduled
        SetupPromotion(promotion);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new RedeemPromotionCommand(
                    promotion.Id, Guid.NewGuid(), Guid.NewGuid(), 100m),
                CancellationToken.None));

        Assert.Contains("Promotion has not started yet.", exception.Message);
        Assert.Equal(0, promotion.CurrentUses);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Redeem_WhenWindowHasEnded_ThrowsConflict()
    {
        var handler = new RedeemPromotionCommandHandler(_promotions.Object, _unitOfWork.Object);
        // the fixture backdates the publish so the campaign exists as Active
        // while its window has already closed
        var promotion = PromotionsFixtures.CreatePromotion(
            status: PromotionStatus.Active,
            endAtUtc: DateTimeOffset.UtcNow.AddDays(-1));
        SetupPromotion(promotion);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new RedeemPromotionCommand(
                    promotion.Id, Guid.NewGuid(), Guid.NewGuid(), 100m),
                CancellationToken.None));

        Assert.Contains("Promotion has ended.", exception.Message);
        Assert.Equal(0, promotion.CurrentUses);
        VerifyNeverSaved(_unitOfWork);
    }
}
