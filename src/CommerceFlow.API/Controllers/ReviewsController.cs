using CommerceFlow.Application.Reviews.Commands.ApproveReview;
using CommerceFlow.Application.Reviews.Commands.CreateReview;
using CommerceFlow.Application.Reviews.Commands.RejectReview;
using CommerceFlow.Application.Reviews.Commands.ReplyToReview;
using CommerceFlow.Application.Reviews.Queries.GetReviewById;
using CommerceFlow.Application.Reviews.Queries.GetReviews;
using CommerceFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

// Thin HTTP adapter: binds request -> Command/Query, returns the result.
// Every state move lives under {id}/... so the intent of the call is explicit
// in the URL (same call as ShipmentsController); reject/reply take their text
// as a JSON string body (same as FailShipment). Moderation is open: approve
// publishes, reject decides a pending review or takes down published content.
[ApiController]
[Route("api/[controller]")]
public sealed class ReviewsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetReviews(
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] ReviewStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetReviewsQuery(productId, customerId, status, pageNumber, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetReviewById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetReviewByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview(
        CreateReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetReviewById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/approve")] // moderator publishes the review
    public async Task<IActionResult> ApproveReview(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ApproveReviewCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")] // decline or take down, reason as JSON string
    public async Task<IActionResult> RejectReview(
        Guid id,
        [FromBody] string reason,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new RejectReviewCommand(id, reason), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reply")] // merchant answers a published review
    public async Task<IActionResult> ReplyToReview(
        Guid id,
        [FromBody] string reply,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new ReplyToReviewCommand(id, reply), cancellationToken);
        return NoContent();
    }
}
