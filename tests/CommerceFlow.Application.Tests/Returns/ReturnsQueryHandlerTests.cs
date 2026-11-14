using CommerceFlow.Application.Returns.Interfaces;
using CommerceFlow.Application.Returns.Queries.GetReturnById;
using CommerceFlow.Application.Returns.Queries.GetReturns;
using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Returns;

public sealed class ReturnsQueryHandlerTests
{
    private readonly Mock<IReturnRequestRepository> _returns = new();

    private void SetupReturn(ReturnRequest request) =>
        _returns
            .Setup(x => x.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

    // ---------- GetReturnById ----------

    [Fact]
    public async Task GetReturnById_WhenReturnDoesNotExist_ReturnsNull()
    {
        var handler = new GetReturnByIdQueryHandler(_returns.Object);

        var result = await handler.Handle(
            new GetReturnByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReturnById_WhenCompleted_MapsAllFieldsAndItems()
    {
        var handler = new GetReturnByIdQueryHandler(_returns.Object);
        var request = ReturnsFixtures.CreateReturnRequest(
            status: ReturnStatus.Completed,
            notes: "Box crushed in transit.");
        SetupReturn(request);

        var dto = await handler.Handle(
            new GetReturnByIdQuery(request.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(request.Id, dto.Id);
        Assert.Equal(ReturnsFixtures.OrderId, dto.OrderId);
        Assert.Equal(ReturnsFixtures.CustomerId, dto.CustomerId);
        Assert.Equal(ReturnStatus.Completed, dto.Status);
        Assert.Equal(ReturnReason.Damaged, dto.Reason);
        Assert.Equal("Box crushed in transit.", dto.Notes);
        Assert.Null(dto.RejectionReason);
        Assert.NotNull(dto.ApprovedAtUtc);
        Assert.NotNull(dto.ReceivedAtUtc);
        Assert.NotNull(dto.CompletedAtUtc);
        Assert.Null(dto.RejectedAtUtc);
        Assert.Null(dto.CancelledAtUtc);
        Assert.Equal(request.CreatedAtUtc, dto.CreatedAtUtc);
        Assert.Equal(request.UpdatedAtUtc, dto.UpdatedAtUtc);

        var item = Assert.Single(dto.Items);
        var entity = Assert.Single(request.Items);
        Assert.Equal(entity.Id, item.Id);
        Assert.Equal(entity.ProductVariantId, item.ProductVariantId);
        Assert.Equal(entity.Quantity, item.Quantity);
    }

    [Fact]
    public async Task GetReturnById_WhenRejected_MapsRejectionReason()
    {
        var handler = new GetReturnByIdQueryHandler(_returns.Object);
        var request = ReturnsFixtures.CreateReturnRequest(
            status: ReturnStatus.Rejected,
            rejectionReason: "Outside the return window.");
        SetupReturn(request);

        var dto = await handler.Handle(
            new GetReturnByIdQuery(request.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(ReturnStatus.Rejected, dto.Status);
        Assert.Equal("Outside the return window.", dto.RejectionReason);
        Assert.NotNull(dto.RejectedAtUtc);
        Assert.Null(dto.ApprovedAtUtc);
        Assert.Null(dto.ReceivedAtUtc);
        Assert.Null(dto.CompletedAtUtc);
    }

    // ---------- GetReturns ----------

    [Fact]
    public async Task GetReturns_WhenNoneExist_ReturnsEmptyPagedResult()
    {
        var handler = new GetReturnsQueryHandler(_returns.Object);
        _returns
            .Setup(x => x.CountAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<ReturnStatus?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _returns
            .Setup(x => x.GetAllAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<ReturnStatus?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReturnRequest>());

        var result = await handler.Handle(new GetReturnsQuery(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetReturns_WithFilters_PassesThemThroughAndMapsItems()
    {
        var handler = new GetReturnsQueryHandler(_returns.Object);
        var customerId = Guid.NewGuid();
        var first = ReturnsFixtures.CreateReturnRequest(
            customerId: customerId, status: ReturnStatus.Pending);
        var second = ReturnsFixtures.CreateReturnRequest(
            customerId: customerId, status: ReturnStatus.Approved);
        _returns
            .Setup(x => x.CountAsync(
                null, customerId, ReturnStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _returns
            .Setup(x => x.GetAllAsync(
                null, customerId, ReturnStatus.Pending, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReturnRequest> { first, second });

        var result = await handler.Handle(
            new GetReturnsQuery(CustomerId: customerId, Status: ReturnStatus.Pending, PageNumber: 2, PageSize: 5),
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(
            new[] { first.Id, second.Id },
            result.Items.Select(r => r.Id));
        Assert.Equal(
            new[] { ReturnStatus.Pending, ReturnStatus.Approved },
            result.Items.Select(r => r.Status));

        _returns.Verify(
            x => x.CountAsync(
                null, customerId, ReturnStatus.Pending, It.IsAny<CancellationToken>()),
            Times.Once);
        _returns.Verify(
            x => x.GetAllAsync(
                null, customerId, ReturnStatus.Pending, 2, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
