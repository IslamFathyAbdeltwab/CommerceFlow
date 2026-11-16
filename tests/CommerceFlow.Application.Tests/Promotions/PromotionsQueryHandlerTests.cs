using CommerceFlow.Application.Promotions.Interfaces;
using CommerceFlow.Application.Promotions.Queries;
using CommerceFlow.Application.Promotions.Queries.GetPromotionById;
using CommerceFlow.Application.Promotions.Queries.GetPromotions;
using CommerceFlow.Domain.Entities.Promotions;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Promotions;

public sealed class PromotionsQueryHandlerTests
{
    private readonly Mock<IPromotionRepository> _promotions = new();

    private void SetupPromotion(Promotion promotion) =>
        _promotions
            .Setup(x => x.GetByIdAsync(promotion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

    // ---------- GetPromotionById ----------

    [Fact]
    public async Task GetPromotionById_WhenPromotionDoesNotExist_ReturnsNull()
    {
        var handler = new GetPromotionByIdQueryHandler(_promotions.Object);

        var result = await handler.Handle(
            new GetPromotionByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result); // controller turns this into 404
    }

    [Fact]
    public async Task GetPromotionById_WhenActive_MapsAllFields()
    {
        var handler = new GetPromotionByIdQueryHandler(_promotions.Object);
        var promotion = PromotionsFixtures.CreatePromotion(
            status: PromotionStatus.Active,
            minOrderAmount: 50,
            maxUsesTotal: 100,
            maxUsesPerCustomer: 2);
        SetupPromotion(promotion);

        var dto = await handler.Handle(
            new GetPromotionByIdQuery(promotion.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(promotion.Id, dto.Id);
        Assert.Equal("SAVE10", dto.Code);
        Assert.Equal("Autumn sale", dto.Name);
        Assert.Equal("Ten percent off.", dto.Description);
        Assert.Equal(DiscountType.Percentage, dto.DiscountType);
        Assert.Equal(10, dto.DiscountValue);
        Assert.Equal(PromotionStatus.Active, dto.Status);
        Assert.Equal(PromotionsFixtures.StartedAtUtc, dto.StartAtUtc);
        Assert.Equal(PromotionsFixtures.EndsAtUtc, dto.EndAtUtc);
        Assert.Equal(50, dto.MinOrderAmount);
        Assert.Equal(100, dto.MaxUsesTotal);
        Assert.Equal(2, dto.MaxUsesPerCustomer);
        Assert.Equal(0, dto.CurrentUses);
        Assert.NotNull(dto.ActivatedAtUtc);
        Assert.Null(dto.PausedAtUtc);
        Assert.Null(dto.ExpiredAtUtc);
        Assert.Equal(promotion.CreatedAtUtc, dto.CreatedAtUtc);
        Assert.Equal(promotion.UpdatedAtUtc, dto.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetPromotionById_WhenDraft_MapsNullLifecycleStamps()
    {
        var handler = new GetPromotionByIdQueryHandler(_promotions.Object);
        var promotion = PromotionsFixtures.CreatePromotion(); // Draft
        SetupPromotion(promotion);

        var dto = await handler.Handle(
            new GetPromotionByIdQuery(promotion.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(PromotionStatus.Draft, dto.Status);
        Assert.Null(dto.ActivatedAtUtc);
        Assert.Null(dto.PausedAtUtc);
        Assert.Null(dto.ExpiredAtUtc);
        Assert.Equal(0, dto.CurrentUses);
    }

    // ---------- GetPromotions ----------

    [Fact]
    public async Task GetPromotions_WhenNoneExist_ReturnsEmptyPagedResult()
    {
        var handler = new GetPromotionsQueryHandler(_promotions.Object);
        _promotions
            .Setup(x => x.CountAsync(
                It.IsAny<string?>(), It.IsAny<PromotionStatus?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _promotions
            .Setup(x => x.GetAllAsync(
                It.IsAny<string?>(), It.IsAny<PromotionStatus?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion>());

        var result = await handler.Handle(new GetPromotionsQuery(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetPromotions_WithFilters_PassesThemThroughAndMapsItems()
    {
        var handler = new GetPromotionsQueryHandler(_promotions.Object);
        var first = PromotionsFixtures.CreatePromotion(code: "SAVE10");
        var second = PromotionsFixtures.CreatePromotion(code: "SAVE20");
        _promotions
            .Setup(x => x.CountAsync(
                "save", PromotionStatus.Draft, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _promotions
            .Setup(x => x.GetAllAsync(
                "save", PromotionStatus.Draft, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion> { first, second });

        var result = await handler.Handle(
            new GetPromotionsQuery(
                Code: "save", Status: PromotionStatus.Draft, PageNumber: 2, PageSize: 5),
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(
            new[] { "SAVE10", "SAVE20" },
            result.Items.Select(p => p.Code)); // repository order preserved
        Assert.All(result.Items, p =>
            Assert.Equal(PromotionStatus.Draft, p.Status));

        _promotions.Verify(
            x => x.CountAsync(
                "save", PromotionStatus.Draft, It.IsAny<CancellationToken>()),
            Times.Once);
        _promotions.Verify(
            x => x.GetAllAsync(
                "save", PromotionStatus.Draft, 2, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
