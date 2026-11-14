using CommerceFlow.Application.Returns.Commands.ApproveReturn;
using CommerceFlow.Application.Returns.Commands.CancelReturn;
using CommerceFlow.Application.Returns.Commands.CompleteReturn;
using CommerceFlow.Application.Returns.Commands.CreateReturnRequest;
using CommerceFlow.Application.Returns.Commands.ReceiveReturn;
using CommerceFlow.Application.Returns.Commands.RejectReturn;
using CommerceFlow.Application.Returns.Queries.GetReturnById;
using CommerceFlow.Application.Returns.Queries.GetReturns;
using CommerceFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

// Thin HTTP adapter: binds request -> Command/Query, returns the result.
// Every state move lives under {id}/... so the intent of the call is explicit
// in the URL (same call as ShipmentsController); the reject endpoint takes
// the merchant's reason as a JSON string body (same as FailShipment).
[ApiController]
[Route("api/[controller]")]
public sealed class ReturnsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetReturns(
        [FromQuery] Guid? orderId = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] ReturnStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetReturnsQuery(orderId, customerId, status, pageNumber, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetReturnById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetReturnByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReturnRequest(
        CreateReturnRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetReturnById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/approve")] // merchant accepts the RMA
    public async Task<IActionResult> ApproveReturn(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ApproveReturnCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")] // merchant declines, reason as JSON string
    public async Task<IActionResult> RejectReturn(
        Guid id,
        [FromBody] string reason,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new RejectReturnCommand(id, reason), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/receive")] // warehouse scanned the goods back in
    public async Task<IActionResult> ReceiveReturn(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ReceiveReturnCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")] // fully processed; refund goes through Payments
    public async Task<IActionResult> CompleteReturn(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new CompleteReturnCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")] // withdrawn while pending or revoked after approval
    public async Task<IActionResult> CancelReturn(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new CancelReturnCommand(id), cancellationToken);
        return NoContent();
    }
}
