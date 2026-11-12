using CommerceFlow.Application.Payments.Commands.CreatePayment;
using CommerceFlow.Application.Payments.Commands.ProcessPayment;
using CommerceFlow.Application.Payments.Commands.RefundPayment;
using CommerceFlow.Application.Payments.Queries.GetPaymentById;
using CommerceFlow.Application.Payments.Queries.GetPayments;
using CommerceFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

// Thin HTTP adapter: binds request -> Command/Query, returns the result.
// The two POST actions that move money live under {id}/... so the intent of
// the call is explicit in the URL.
[ApiController]
[Route("api/[controller]")]
public sealed class PaymentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPayments(
        [FromQuery] Guid? orderId = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetPaymentsQuery(orderId, customerId, status, pageNumber, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPaymentById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetPaymentByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayment(
        CreatePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        // Replay-safe: a retried create with the same idempotency key returns
        // the original payment's id instead of creating a second one.
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPaymentById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/process")] // money moves: charge the provider
    public async Task<IActionResult> ProcessPayment(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ProcessPaymentCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/refund")] // money moves back: refund the provider
    public async Task<IActionResult> RefundPayment(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new RefundPaymentCommand(id), cancellationToken);
        return Ok(result);
    }
}
