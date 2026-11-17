using CommerceFlow.Application.Notifications.Commands.FailNotification;
using CommerceFlow.Application.Notifications.Commands.MarkNotificationRead;
using CommerceFlow.Application.Notifications.Commands.MarkNotificationSent;
using CommerceFlow.Application.Notifications.Commands.RetryNotification;
using CommerceFlow.Application.Notifications.Commands.SendNotification;
using CommerceFlow.Application.Notifications.Queries.GetNotificationById;
using CommerceFlow.Application.Notifications.Queries.GetNotifications;
using CommerceFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

// Thin HTTP adapter: binds request -> Command/Query, returns the result.
// Confirmations live under {id}/... because the transport is at-least-once:
// a redelivered confirm answers 204 (idempotent no-op), never 409 - only
// states that can never reach the target refuse with a conflict. Send is the
// one POST that answers 201: the caller needs the id of the row the queue
// now carries. Notifications are history - there is no DELETE.
[ApiController]
[Route("api/[controller]")]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet] // recipientId = personal feed, status = "what is stuck" admin view
    public async Task<IActionResult> GetNotifications(
        [FromQuery] Guid? recipientId = null,
        [FromQuery] NotificationStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetNotificationsQuery(recipientId, status, pageNumber, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetNotificationById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetNotificationByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost] // create + hand to the transport (publish before commit)
    public async Task<IActionResult> SendNotification(
        SendNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetNotificationById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/sent")] // consumer: channel accepted the message
    public async Task<IActionResult> MarkSent(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new MarkNotificationSentCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/failed")] // consumer/sender: delivery died, reason as JSON string
    public async Task<IActionResult> Fail(
        Guid id,
        [FromBody] string reason,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new FailNotificationCommand(id, reason), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/read")] // recipient opened it (in-app read receipt)
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/retry")] // operator: Failed -> Queued, fresh publish
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new RetryNotificationCommand(id), cancellationToken);
        return NoContent();
    }
}
