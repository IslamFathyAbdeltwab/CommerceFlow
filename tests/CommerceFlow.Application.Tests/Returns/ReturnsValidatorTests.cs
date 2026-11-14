using CommerceFlow.Application.Returns.Commands.ApproveReturn;
using CommerceFlow.Application.Returns.Commands.CancelReturn;
using CommerceFlow.Application.Returns.Commands.CompleteReturn;
using CommerceFlow.Application.Returns.Commands.CreateReturnRequest;
using CommerceFlow.Application.Returns.Commands.ReceiveReturn;
using CommerceFlow.Application.Returns.Commands.RejectReturn;
using CommerceFlow.Application.Returns.Queries.GetReturns;
using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Tests.Returns;

public sealed class ReturnsValidatorTests
{
    private static string TooLong(int length) => new('x', length);

    // ---------- CreateReturnRequest ----------

    [Fact]
    public void Create_WithValidInput_IsValid()
    {
        var result = new CreateReturnRequestCommandValidator()
            .Validate(ReturnsFixtures.ValidCreateCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_WithEmptyOrderId_IsInvalid()
    {
        var result = new CreateReturnRequestCommandValidator()
            .Validate(ReturnsFixtures.ValidCreateCommand() with { OrderId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateReturnRequestCommand.OrderId));
    }

    [Fact]
    public void Create_WithUnknownReason_IsInvalid()
    {
        var result = new CreateReturnRequestCommandValidator()
            .Validate(ReturnsFixtures.ValidCreateCommand() with { Reason = (ReturnReason)99 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateReturnRequestCommand.Reason));
    }

    [Fact]
    public void Create_WithTooLongNotes_IsInvalid()
    {
        var result = new CreateReturnRequestCommandValidator()
            .Validate(ReturnsFixtures.ValidCreateCommand() with
            {
                Notes = TooLong(ReturnRequest.NotesMaxLength + 1)
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateReturnRequestCommand.Notes));
    }

    [Fact]
    public void Create_WithNoItems_IsInvalid()
    {
        var result = new CreateReturnRequestCommandValidator()
            .Validate(ReturnsFixtures.ValidCreateCommand() with { Items = [] });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateReturnRequestCommand.Items));
    }

    [Fact]
    public void Create_WithEmptyItemVariantId_IsInvalid()
    {
        var result = new CreateReturnRequestCommandValidator()
            .Validate(ReturnsFixtures.ValidCreateCommand(productVariantId: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName!.Contains(nameof(CreateReturnRequestItemDto.ProductVariantId)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveItemQuantity_IsInvalid(int quantity)
    {
        var result = new CreateReturnRequestCommandValidator()
            .Validate(ReturnsFixtures.ValidCreateCommand(quantity: quantity));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName!.Contains(nameof(CreateReturnRequestItemDto.Quantity)));
    }

    // ---------- ApproveReturn / ReceiveReturn / CompleteReturn / CancelReturn ----------

    [Fact]
    public void Approve_WithValidInput_IsValid()
    {
        var result = new ApproveReturnCommandValidator()
            .Validate(new ApproveReturnCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Approve_WithEmptyReturnId_IsInvalid()
    {
        var result = new ApproveReturnCommandValidator()
            .Validate(new ApproveReturnCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(ApproveReturnCommand.ReturnId));
    }

    [Fact]
    public void Receive_WithEmptyReturnId_IsInvalid()
    {
        var result = new ReceiveReturnCommandValidator()
            .Validate(new ReceiveReturnCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(ReceiveReturnCommand.ReturnId));
    }

    [Fact]
    public void Complete_WithEmptyReturnId_IsInvalid()
    {
        var result = new CompleteReturnCommandValidator()
            .Validate(new CompleteReturnCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CompleteReturnCommand.ReturnId));
    }

    [Fact]
    public void Cancel_WithEmptyReturnId_IsInvalid()
    {
        var result = new CancelReturnCommandValidator()
            .Validate(new CancelReturnCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CancelReturnCommand.ReturnId));
    }

    // ---------- RejectReturn ----------

    [Fact]
    public void Reject_WithValidInput_IsValid()
    {
        var result = new RejectReturnCommandValidator()
            .Validate(new RejectReturnCommand(Guid.NewGuid(), "Outside the window."));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Reject_WithEmptyReturnId_IsInvalid()
    {
        var result = new RejectReturnCommandValidator()
            .Validate(new RejectReturnCommand(Guid.Empty, "Outside the window."));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(RejectReturnCommand.ReturnId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithEmptyReason_IsInvalid(string reason)
    {
        var result = new RejectReturnCommandValidator()
            .Validate(new RejectReturnCommand(Guid.NewGuid(), reason));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(RejectReturnCommand.Reason));
    }

    [Fact]
    public void Reject_WithTooLongReason_IsInvalid()
    {
        var result = new RejectReturnCommandValidator()
            .Validate(new RejectReturnCommand(
                Guid.NewGuid(), TooLong(ReturnRequest.RejectionReasonMaxLength + 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(RejectReturnCommand.Reason));
    }

    // ---------- GetReturns ----------

    [Fact]
    public void GetReturns_WithDefaults_IsValid()
    {
        var result = new GetReturnsQueryValidator().Validate(new GetReturnsQuery());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1001, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void GetReturns_WithPagingOutOfRange_IsInvalid(int pageNumber, int pageSize)
    {
        var result = new GetReturnsQueryValidator()
            .Validate(new GetReturnsQuery(PageNumber: pageNumber, PageSize: pageSize));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(GetReturnsQuery.PageNumber)
            || e.PropertyName == nameof(GetReturnsQuery.PageSize));
    }
}
