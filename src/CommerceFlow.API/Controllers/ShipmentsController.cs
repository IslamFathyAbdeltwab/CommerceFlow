using CommerceFlow.Application.Shipping.Commands.CancelShipment;
using CommerceFlow.Application.Shipping.Commands.CreateShipment;
using CommerceFlow.Application.Shipping.Commands.DeliverShipment;
using CommerceFlow.Application.Shipping.Commands.FailShipment;
using CommerceFlow.Application.Shipping.Commands.MarkShipmentInTransit;
using CommerceFlow.Application.Shipping.Commands.ReturnShipment;
using CommerceFlow.Application.Shipping.Commands.ShipShipment;
using CommerceFlow.Application.Shipping.Queries.GetShipmentById;
using CommerceFlow.Application.Shipping.Queries.GetShipments;
using CommerceFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

// Thin HTTP adapter: binds request -> Command/Query, returns the result.
// Every state move lives under {id}/... so the intent of the call is explicit
// in the URL (same call as PaymentsController); the fail endpoint takes the
// carrier's reason as a JSON string body (same as SetVariantAttributeValue).
[ApiController]
[Route("api/[controller]")]
public sealed class ShipmentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetShipments(
        [FromQuery] Guid? orderId = null,
        [FromQuery] ShipmentStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetShipmentsQuery(orderId, status, pageNumber, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetShipmentById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetShipmentByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateShipment(
        CreateShipmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetShipmentById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/ship")] // hand the box to the carrier
    public async Task<IActionResult> ShipShipment(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ShipShipmentCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/in-transit")] // carrier scanned it onto the truck
    public async Task<IActionResult> MarkShipmentInTransit(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new MarkShipmentInTransitCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deliver")] // carrier scanned it as delivered
    public async Task<IActionResult> DeliverShipment(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new DeliverShipmentCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/fail")] // carrier webhook: a delivery attempt failed
    public async Task<IActionResult> FailShipment(
        Guid id,
        [FromBody] string reason,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new FailShipmentCommand(id, reason), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/return")] // package coming back after a failed attempt
    public async Task<IActionResult> ReturnShipment(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ReturnShipmentCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")] // stopped before hand-off (pending only)
    public async Task<IActionResult> CancelShipment(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new CancelShipmentCommand(id), cancellationToken);
        return NoContent();
    }
}
