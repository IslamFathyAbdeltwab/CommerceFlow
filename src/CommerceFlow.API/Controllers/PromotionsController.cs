using CommerceFlow.Application.Promotions.Commands.ActivatePromotion;
using CommerceFlow.Application.Promotions.Commands.CreatePromotion;
using CommerceFlow.Application.Promotions.Commands.ExpirePromotion;
using CommerceFlow.Application.Promotions.Commands.PausePromotion;
using CommerceFlow.Application.Promotions.Commands.RedeemPromotion;
using CommerceFlow.Application.Promotions.Commands.UpdatePromotion;
using CommerceFlow.Application.Promotions.Queries.GetPromotionById;
using CommerceFlow.Application.Promotions.Queries.GetPromotions;
using CommerceFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

// Thin HTTP adapter: binds request -> Command/Query, returns the result.
// Lifecycle moves live under {id}/... so the intent of the call is explicit
// in the URL (same call as ReturnsController). Redeem is the one POST that
// answers 200 with a body instead of 204: the caller needs the discount the
// campaign just took off the order. Campaigns are ended (expire), never
// deleted - the usage rows are an audit trail.
[ApiController]
[Route("api/[controller]")]
public sealed class PromotionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPromotions(
        [FromQuery] string? code = null,
        [FromQuery] PromotionStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetPromotionsQuery(code, status, pageNumber, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPromotionById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetPromotionByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost] // created as Draft - Activate publishes (same as Products)
    public async Task<IActionResult> CreatePromotion(
        CreatePromotionCommand command,
        CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPromotionById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")] // text/window/limits only, while draft or paused
    public async Task<IActionResult> UpdatePromotion(
        Guid id,
        UpdatePromotionCommand command,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { PromotionId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")] // publish a draft or resume a paused one
    public async Task<IActionResult> ActivatePromotion(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ActivatePromotionCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/pause")] // temporary offline hold - resumable
    public async Task<IActionResult> PausePromotion(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new PausePromotionCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/expire")] // force-end; terminal state
    public async Task<IActionResult> ExpirePromotion(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ExpirePromotionCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/redeem")] // apply to one order -> discount taken
    public async Task<IActionResult> RedeemPromotion(
        Guid id,
        RedeemPromotionCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(command with { PromotionId = id }, cancellationToken);
        return Ok(result);
    }
}
