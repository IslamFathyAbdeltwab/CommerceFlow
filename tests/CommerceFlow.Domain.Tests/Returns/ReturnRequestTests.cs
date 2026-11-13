using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Returns;

public class ReturnRequestTests
{
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static ReturnRequest NewReturn(string? notes = null) =>
        ReturnRequest.Create(OrderId, CustomerId, ReturnReason.Damaged, notes);

    private static ReturnRequest ItemedReturn()
    {
        var request = NewReturn();
        request.AddItem(Guid.NewGuid(), 1);
        return request;
    }

    private static ReturnRequest ApprovedReturn()
    {
        var request = ItemedReturn();
        request.Approve();
        return request;
    }

    private static ReturnRequest ReceivedReturn()
    {
        var request = ApprovedReturn();
        request.MarkReceived();
        return request;
    }

    private static ReturnRequest CompletedReturn()
    {
        var request = ReceivedReturn();
        request.Complete();
        return request;
    }

    private static ReturnRequest RejectedReturn()
    {
        var request = NewReturn();
        request.Reject("Outside the 30-day window.");
        return request;
    }

    private static ReturnRequest CancelledReturn()
    {
        var request = NewReturn();
        request.Cancel();
        return request;
    }

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_CreatesPendingRequestForOrder()
    {
        var request = NewReturn();

        Assert.Equal(OrderId, request.OrderId);
        Assert.Equal(CustomerId, request.CustomerId);
        Assert.Equal(ReturnStatus.Pending, request.Status);
        Assert.Equal(ReturnReason.Damaged, request.Reason);
        Assert.Null(request.Notes);
        Assert.Null(request.RejectionReason);
        Assert.Null(request.ApprovedAtUtc);
        Assert.Null(request.RejectedAtUtc);
        Assert.Null(request.ReceivedAtUtc);
        Assert.Null(request.CompletedAtUtc);
        Assert.Null(request.CancelledAtUtc);
        Assert.Empty(request.Items);
        Assert.NotEqual(Guid.Empty, request.Id);
    }

    [Fact]
    public void Create_SetsAuditTimestamps()
    {
        var request = NewReturn();

        Assert.NotEqual(default, request.CreatedAtUtc);
        Assert.NotEqual(default, request.UpdatedAtUtc);
        Assert.True(request.UpdatedAtUtc >= request.CreatedAtUtc);
    }

    [Fact]
    public void Create_EmptyOrderId_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            ReturnRequest.Create(Guid.Empty, CustomerId, ReturnReason.Damaged));

        Assert.Equal("Order ID is required.", exception.Message);
    }

    [Fact]
    public void Create_EmptyCustomerId_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            ReturnRequest.Create(OrderId, Guid.Empty, ReturnReason.Damaged));

        Assert.Equal("Customer ID is required.", exception.Message);
    }

    [Fact]
    public void Create_UnknownReason_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            ReturnRequest.Create(OrderId, CustomerId, (ReturnReason)99));

        Assert.Equal("Return reason is required.", exception.Message);
    }

    [Fact]
    public void Create_WithNotes_StoresTrimmed()
    {
        var request = NewReturn(notes: "  Box was crushed in transit.  ");

        Assert.Equal("Box was crushed in transit.", request.Notes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_OptionalNotes_EmptyOrNullBecomesNull(string? notes)
    {
        Assert.Null(NewReturn(notes).Notes);
    }

    [Fact]
    public void Create_NotesTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewReturn(notes: new string('n', ReturnRequest.NotesMaxLength + 1)));

        Assert.Equal("Notes cannot exceed 500 characters.", exception.Message);
    }

    // ---------- AddItem ----------

    [Fact]
    public void AddItem_Valid_AddsItemToRequest()
    {
        var request = NewReturn();
        var variantId = Guid.NewGuid();

        request.AddItem(variantId, quantity: 2);

        var item = Assert.Single(request.Items);
        Assert.Equal(variantId, item.ProductVariantId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(request.Id, item.ReturnRequestId);
        Assert.NotEqual(Guid.Empty, item.Id);
    }

    [Fact]
    public void AddItem_TwoDistinctVariants_BothAllowed()
    {
        var request = NewReturn();

        request.AddItem(Guid.NewGuid(), 1);
        request.AddItem(Guid.NewGuid(), 2);

        Assert.Equal(2, request.Items.Count);
    }

    [Fact]
    public void AddItem_EmptyVariantId_Throws()
    {
        var request = NewReturn();

        var exception = Assert.Throws<DomainException>(() =>
            request.AddItem(Guid.Empty, 1));

        Assert.Equal("Product variant ID is required.", exception.Message);
        Assert.Empty(request.Items);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_NonPositiveQuantity_Throws(int quantity)
    {
        var request = NewReturn();

        var exception = Assert.Throws<DomainException>(() =>
            request.AddItem(Guid.NewGuid(), quantity));

        Assert.Equal("Quantity must be positive.", exception.Message);
        Assert.Empty(request.Items);
    }

    [Fact]
    public void AddItem_DuplicateVariant_ThrowsAndKeepsOriginal()
    {
        var request = NewReturn();
        var variantId = Guid.NewGuid();
        request.AddItem(variantId, 1);

        var exception = Assert.Throws<DomainException>(() =>
            request.AddItem(variantId, 5));

        Assert.Equal("Return request already contains this variant.", exception.Message);
        Assert.Single(request.Items);
        Assert.Equal(1, request.Items.First().Quantity); // original kept
    }

    [Fact]
    public void AddItem_AfterApproval_ThrowsAndItemsUnchanged()
    {
        var request = ApprovedReturn();
        var countBefore = request.Items.Count;

        var exception = Assert.Throws<DomainException>(() =>
            request.AddItem(Guid.NewGuid(), 1));

        Assert.Equal(
            "Return items can only be added while the request is pending.",
            exception.Message);
        Assert.Equal(countBefore, request.Items.Count);
    }

    [Fact]
    public void AddItem_UpToMaxItems_IsAllowed()
    {
        var request = NewReturn();

        for (var i = 0; i < ReturnRequest.MaxItems; i++)
            request.AddItem(Guid.NewGuid(), 1);

        Assert.Equal(ReturnRequest.MaxItems, request.Items.Count);
    }

    [Fact]
    public void AddItem_BeyondMaxItems_Throws()
    {
        var request = NewReturn();
        for (var i = 0; i < ReturnRequest.MaxItems; i++)
            request.AddItem(Guid.NewGuid(), 1);

        var exception = Assert.Throws<DomainException>(() =>
            request.AddItem(Guid.NewGuid(), 1));

        Assert.Equal("Return request cannot contain more than 100 items.", exception.Message);
        Assert.Equal(ReturnRequest.MaxItems, request.Items.Count);
    }

    // ---------- Approve ----------

    [Fact]
    public void Approve_FromPending_ApprovesAndSetsTimestamp()
    {
        var request = ItemedReturn();

        request.Approve();

        Assert.Equal(ReturnStatus.Approved, request.Status);
        Assert.NotNull(request.ApprovedAtUtc);
        Assert.True(request.ApprovedAtUtc >= request.CreatedAtUtc);
        Assert.Null(request.RejectedAtUtc);
        Assert.Null(request.ReceivedAtUtc);
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_IsNoOp()
    {
        var request = ApprovedReturn();
        var approvedAt = request.ApprovedAtUtc;
        var updatedAt = request.UpdatedAtUtc;

        request.Approve();

        Assert.Equal(ReturnStatus.Approved, request.Status);
        Assert.Equal(approvedAt, request.ApprovedAtUtc);
        Assert.Equal(updatedAt, request.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void Approve_FromRejected_ThrowsAndKeepsRejection()
    {
        var request = RejectedReturn();

        var exception = Assert.Throws<DomainException>(() => request.Approve());

        Assert.Equal(
            "Return request cannot move from Rejected to Approved.", exception.Message);
        Assert.Equal(ReturnStatus.Rejected, request.Status);
        Assert.Equal("Outside the 30-day window.", request.RejectionReason);
    }

    [Fact]
    public void Approve_FromReceived_Throws()
    {
        var request = ReceivedReturn();

        var exception = Assert.Throws<DomainException>(() => request.Approve());

        Assert.Equal(
            "Return request cannot move from Received to Approved.", exception.Message);
        Assert.Equal(ReturnStatus.Received, request.Status);
    }

    [Fact]
    public void Approve_FromCompleted_ThrowsAndKeepsCompletion()
    {
        var request = CompletedReturn();
        var completedAt = request.CompletedAtUtc;

        var exception = Assert.Throws<DomainException>(() => request.Approve());

        Assert.Equal(
            "Return request cannot move from Completed to Approved.", exception.Message);
        Assert.Equal(ReturnStatus.Completed, request.Status);
        Assert.Equal(completedAt, request.CompletedAtUtc);
    }

    [Fact]
    public void Approve_FromCancelled_Throws()
    {
        var request = CancelledReturn();

        var exception = Assert.Throws<DomainException>(() => request.Approve());

        Assert.Equal(
            "Return request cannot move from Cancelled to Approved.", exception.Message);
        Assert.Equal(ReturnStatus.Cancelled, request.Status);
    }

    // ---------- Reject ----------

    [Fact]
    public void Reject_FromPending_RejectsAndStoresTrimmedReason()
    {
        var request = ItemedReturn();

        request.Reject("  Outside the return window.  ");

        Assert.Equal(ReturnStatus.Rejected, request.Status);
        Assert.Equal("Outside the return window.", request.RejectionReason); // trimmed
        Assert.NotNull(request.RejectedAtUtc);
        Assert.Null(request.ApprovedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_MissingReason_ThrowsAndStaysPending(string reason)
    {
        var request = ItemedReturn();

        var exception = Assert.Throws<DomainException>(() => request.Reject(reason));

        Assert.Equal("Rejection reason is required.", exception.Message);
        Assert.Equal(ReturnStatus.Pending, request.Status);
        Assert.Null(request.RejectionReason);
    }

    [Fact]
    public void Reject_ReasonTooLong_Throws()
    {
        var request = ItemedReturn();

        var exception = Assert.Throws<DomainException>(() =>
            request.Reject(new string('r', ReturnRequest.RejectionReasonMaxLength + 1)));

        Assert.Equal("Rejection reason cannot exceed 500 characters.", exception.Message);
        Assert.Equal(ReturnStatus.Pending, request.Status);
        Assert.Null(request.RejectionReason);
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_IsNoOpAndKeepsOriginalReason()
    {
        var request = RejectedReturn();
        var rejectedAt = request.RejectedAtUtc;
        var updatedAt = request.UpdatedAtUtc;

        request.Reject("A different later reason");

        Assert.Equal(ReturnStatus.Rejected, request.Status);
        Assert.Equal("Outside the 30-day window.", request.RejectionReason); // original kept
        Assert.Equal(rejectedAt, request.RejectedAtUtc);
        Assert.Equal(updatedAt, request.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void Reject_FromApproved_ThrowsAndStaysApproved()
    {
        var request = ApprovedReturn();

        var exception = Assert.Throws<DomainException>(() => request.Reject("Changed our mind."));

        Assert.Equal(
            "Return request cannot move from Approved to Rejected.", exception.Message);
        Assert.Equal(ReturnStatus.Approved, request.Status);
        Assert.Null(request.RejectionReason);
    }

    [Fact]
    public void Reject_FromReceived_Throws()
    {
        var request = ReceivedReturn();

        var exception = Assert.Throws<DomainException>(() => request.Reject("Late notice."));

        Assert.Equal(
            "Return request cannot move from Received to Rejected.", exception.Message);
        Assert.Equal(ReturnStatus.Received, request.Status);
    }

    [Fact]
    public void Reject_FromCompleted_ThrowsAndKeepsCompletion()
    {
        var request = CompletedReturn();

        var exception = Assert.Throws<DomainException>(() => request.Reject("Late notice."));

        Assert.Equal(
            "Return request cannot move from Completed to Rejected.", exception.Message);
        Assert.Equal(ReturnStatus.Completed, request.Status);
        Assert.Null(request.RejectionReason);
    }

    [Fact]
    public void Reject_FromCancelled_Throws()
    {
        var request = CancelledReturn();

        var exception = Assert.Throws<DomainException>(() => request.Reject("Late notice."));

        Assert.Equal(
            "Return request cannot move from Cancelled to Rejected.", exception.Message);
        Assert.Equal(ReturnStatus.Cancelled, request.Status);
    }

    // ---------- MarkReceived ----------

    [Fact]
    public void MarkReceived_FromApproved_MarksReceivedAndSetsTimestamp()
    {
        var request = ApprovedReturn();

        request.MarkReceived();

        Assert.Equal(ReturnStatus.Received, request.Status);
        Assert.NotNull(request.ReceivedAtUtc);
        Assert.True(request.ReceivedAtUtc >= request.ApprovedAtUtc);
        Assert.Null(request.CompletedAtUtc);
    }

    [Fact]
    public void MarkReceived_WhenAlreadyReceived_IsNoOp()
    {
        var request = ReceivedReturn();
        var receivedAt = request.ReceivedAtUtc;
        var updatedAt = request.UpdatedAtUtc;

        request.MarkReceived();

        Assert.Equal(ReturnStatus.Received, request.Status);
        Assert.Equal(receivedAt, request.ReceivedAtUtc);
        Assert.Equal(updatedAt, request.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void MarkReceived_FromPending_ThrowsAndStaysPending()
    {
        var request = ItemedReturn();

        var exception = Assert.Throws<DomainException>(() => request.MarkReceived());

        Assert.Equal(
            "Return request cannot move from Pending to Received.", exception.Message);
        Assert.Equal(ReturnStatus.Pending, request.Status);
        Assert.Null(request.ReceivedAtUtc);
    }

    [Fact]
    public void MarkReceived_OnApprovedRequestWithoutItems_Throws()
    {
        var request = NewReturn();
        request.Approve(); // nothing to receive

        var exception = Assert.Throws<DomainException>(() => request.MarkReceived());

        Assert.Equal(
            "Return request must contain at least one item before it can be received.",
            exception.Message);
        Assert.Equal(ReturnStatus.Approved, request.Status); // nothing moved
        Assert.Null(request.ReceivedAtUtc);
    }

    [Fact]
    public void MarkReceived_FromRejected_Throws()
    {
        var request = RejectedReturn();

        var exception = Assert.Throws<DomainException>(() => request.MarkReceived());

        Assert.Equal(
            "Return request cannot move from Rejected to Received.", exception.Message);
        Assert.Equal(ReturnStatus.Rejected, request.Status);
    }

    [Fact]
    public void MarkReceived_FromCompleted_Throws()
    {
        var request = CompletedReturn();

        var exception = Assert.Throws<DomainException>(() => request.MarkReceived());

        Assert.Equal(
            "Return request cannot move from Completed to Received.", exception.Message);
        Assert.Equal(ReturnStatus.Completed, request.Status);
    }

    [Fact]
    public void MarkReceived_FromCancelled_Throws()
    {
        var request = CancelledReturn();

        var exception = Assert.Throws<DomainException>(() => request.MarkReceived());

        Assert.Equal(
            "Return request cannot move from Cancelled to Received.", exception.Message);
        Assert.Equal(ReturnStatus.Cancelled, request.Status);
    }

    // ---------- Complete ----------

    [Fact]
    public void Complete_FromReceived_CompletesAndSetsTimestamp()
    {
        var request = ReceivedReturn();

        request.Complete();

        Assert.Equal(ReturnStatus.Completed, request.Status);
        Assert.NotNull(request.CompletedAtUtc);
        Assert.True(request.CompletedAtUtc >= request.ReceivedAtUtc);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_IsNoOp()
    {
        var request = CompletedReturn();
        var completedAt = request.CompletedAtUtc;
        var updatedAt = request.UpdatedAtUtc;

        request.Complete();

        Assert.Equal(ReturnStatus.Completed, request.Status);
        Assert.Equal(completedAt, request.CompletedAtUtc);
        Assert.Equal(updatedAt, request.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void Complete_FromPending_Throws()
    {
        var request = ItemedReturn();

        var exception = Assert.Throws<DomainException>(() => request.Complete());

        Assert.Equal(
            "Return request cannot move from Pending to Completed.", exception.Message);
        Assert.Equal(ReturnStatus.Pending, request.Status);
        Assert.Null(request.CompletedAtUtc);
    }

    [Fact]
    public void Complete_FromApproved_ThrowsAndStaysApproved()
    {
        var request = ApprovedReturn();

        var exception = Assert.Throws<DomainException>(() => request.Complete());

        Assert.Equal(
            "Return request cannot move from Approved to Completed.", exception.Message);
        Assert.Equal(ReturnStatus.Approved, request.Status);
        Assert.Null(request.CompletedAtUtc);
    }

    [Fact]
    public void Complete_FromRejected_Throws()
    {
        var request = RejectedReturn();

        var exception = Assert.Throws<DomainException>(() => request.Complete());

        Assert.Equal(
            "Return request cannot move from Rejected to Completed.", exception.Message);
        Assert.Equal(ReturnStatus.Rejected, request.Status);
    }

    [Fact]
    public void Complete_FromCancelled_Throws()
    {
        var request = CancelledReturn();

        var exception = Assert.Throws<DomainException>(() => request.Complete());

        Assert.Equal(
            "Return request cannot move from Cancelled to Completed.", exception.Message);
        Assert.Equal(ReturnStatus.Cancelled, request.Status);
    }

    // ---------- Cancel ----------

    [Fact]
    public void Cancel_FromPending_CancelsAndSetsTimestamp()
    {
        var request = ItemedReturn();

        request.Cancel();

        Assert.Equal(ReturnStatus.Cancelled, request.Status);
        Assert.NotNull(request.CancelledAtUtc);
        Assert.True(request.CancelledAtUtc >= request.CreatedAtUtc);
        Assert.Null(request.ApprovedAtUtc);
    }

    [Fact]
    public void Cancel_FromApproved_MerchantRevokesAndSetsTimestamp()
    {
        var request = ApprovedReturn();

        request.Cancel();

        Assert.Equal(ReturnStatus.Cancelled, request.Status);
        Assert.NotNull(request.CancelledAtUtc);
        Assert.NotNull(request.ApprovedAtUtc); // audit trail kept
        Assert.Null(request.ReceivedAtUtc);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_IsNoOp()
    {
        var request = CancelledReturn();
        var cancelledAt = request.CancelledAtUtc;
        var updatedAt = request.UpdatedAtUtc;

        request.Cancel();

        Assert.Equal(ReturnStatus.Cancelled, request.Status);
        Assert.Equal(cancelledAt, request.CancelledAtUtc);
        Assert.Equal(updatedAt, request.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void Cancel_FromReceived_ThrowsAndStaysReceived()
    {
        var request = ReceivedReturn();
        var receivedAt = request.ReceivedAtUtc;

        var exception = Assert.Throws<DomainException>(() => request.Cancel());

        Assert.Equal(
            "Return request cannot move from Received to Cancelled.", exception.Message);
        Assert.Equal(ReturnStatus.Received, request.Status);
        Assert.Equal(receivedAt, request.ReceivedAtUtc);
        Assert.Null(request.CancelledAtUtc);
    }

    [Fact]
    public void Cancel_FromRejected_Throws()
    {
        var request = RejectedReturn();

        var exception = Assert.Throws<DomainException>(() => request.Cancel());

        Assert.Equal(
            "Return request cannot move from Rejected to Cancelled.", exception.Message);
        Assert.Equal(ReturnStatus.Rejected, request.Status);
    }

    [Fact]
    public void Cancel_FromCompleted_ThrowsAndKeepsCompletion()
    {
        var request = CompletedReturn();

        var exception = Assert.Throws<DomainException>(() => request.Cancel());

        Assert.Equal(
            "Return request cannot move from Completed to Cancelled.", exception.Message);
        Assert.Equal(ReturnStatus.Completed, request.Status);
        Assert.Null(request.CancelledAtUtc);
    }

    // ---------- Terminal states ----------

    [Fact]
    public void RejectedRequest_EveryFurtherTransition_ThrowsOrNoOps()
    {
        var request = RejectedReturn();

        Assert.Throws<DomainException>(() => request.Approve());
        Assert.Throws<DomainException>(() => request.MarkReceived());
        Assert.Throws<DomainException>(() => request.Complete());
        Assert.Throws<DomainException>(() => request.Cancel());

        request.Reject("late duplicate"); // already rejected -> no-op, reason kept

        Assert.Equal(ReturnStatus.Rejected, request.Status);
        Assert.Equal("Outside the 30-day window.", request.RejectionReason); // untouched
    }

    [Fact]
    public void CompletedRequest_EveryFurtherTransition_Throws()
    {
        var request = CompletedReturn();

        Assert.Throws<DomainException>(() => request.Approve());
        Assert.Throws<DomainException>(() => request.Reject("late notice"));
        Assert.Throws<DomainException>(() => request.MarkReceived());
        Assert.Throws<DomainException>(() => request.Cancel());

        Assert.Equal(ReturnStatus.Completed, request.Status);
        Assert.NotNull(request.CompletedAtUtc); // untouched
        Assert.Null(request.RejectionReason);
    }

    [Fact]
    public void CancelledRequest_EveryFurtherTransition_Throws()
    {
        var request = CancelledReturn();

        Assert.Throws<DomainException>(() => request.Approve());
        Assert.Throws<DomainException>(() => request.Reject("late notice"));
        Assert.Throws<DomainException>(() => request.MarkReceived());
        Assert.Throws<DomainException>(() => request.Complete());

        Assert.Equal(ReturnStatus.Cancelled, request.Status);
        Assert.NotNull(request.CancelledAtUtc); // untouched
    }

    [Fact]
    public void CancelledRequest_ItemsCannotBeAdded()
    {
        var request = CancelledReturn();

        var exception = Assert.Throws<DomainException>(() =>
            request.AddItem(Guid.NewGuid(), 1));

        Assert.Equal(
            "Return items can only be added while the request is pending.",
            exception.Message);
        Assert.Empty(request.Items);
    }
}
