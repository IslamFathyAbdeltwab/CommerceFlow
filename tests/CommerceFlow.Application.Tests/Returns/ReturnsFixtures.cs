using CommerceFlow.Application.Returns.Commands.CreateReturnRequest;
using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Tests.Returns;

// Shared factories for building return requests (and the create command).
// The fixture puts at least one item in the basket by default because
// MarkReceived refuses an empty basket; pass withItem: false only to
// exercise that guard.
internal static class ReturnsFixtures
{
    public static readonly Guid OrderId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid CustomerId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid VariantId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    public static ReturnRequest CreateReturnRequest(
        Guid? orderId = null,
        Guid? customerId = null,
        ReturnStatus status = ReturnStatus.Pending,
        Guid? productVariantId = null,
        int quantity = 2,
        ReturnReason reason = ReturnReason.Damaged,
        string? notes = null,
        string? rejectionReason = null,
        bool withItem = true)
    {
        var request = ReturnRequest.Create(
            orderId ?? OrderId,
            customerId ?? CustomerId,
            reason,
            notes);

        if (withItem)
            request.AddItem(productVariantId ?? VariantId, quantity);

        switch (status)
        {
            case ReturnStatus.Pending:
                break;
            case ReturnStatus.Approved:
                request.Approve();
                break;
            case ReturnStatus.Rejected:
                request.Reject(rejectionReason ?? "Outside the return window.");
                break;
            case ReturnStatus.Received:
                request.Approve();
                request.MarkReceived();
                break;
            case ReturnStatus.Completed:
                request.Approve();
                request.MarkReceived();
                request.Complete();
                break;
            case ReturnStatus.Cancelled:
                request.Cancel();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        return request;
    }

    public static CreateReturnRequestCommand ValidCreateCommand(
        Guid? orderId = null,
        Guid? productVariantId = null,
        int quantity = 1,
        ReturnReason reason = ReturnReason.Damaged,
        string? notes = null) =>
        new(
            orderId ?? OrderId,
            reason,
            [new CreateReturnRequestItemDto(productVariantId ?? VariantId, quantity)],
            notes);
}
