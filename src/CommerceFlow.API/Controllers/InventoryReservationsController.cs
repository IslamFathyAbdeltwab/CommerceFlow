using CommerceFlow.Application.Inventory.Reservations.Commands.ConsumeInventoryReservation;
using CommerceFlow.Application.Inventory.Reservations.Commands.CreateInventoryReservation;
using CommerceFlow.Application.Inventory.Reservations.Commands.ExpireInventoryReservation;
using CommerceFlow.Application.Inventory.Reservations.Commands.ReleaseInventoryReservation;
using CommerceFlow.Application.Inventory.Reservations.Queries.GetInventoryReservationById;
using CommerceFlow.Application.Inventory.Reservations.Queries.GetInventoryReservations;
using CommerceFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InventoryReservationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetReservations(
        [FromQuery] Guid? orderId = null,
        [FromQuery] InventoryReservationStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetInventoryReservationsQuery(orderId, status, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetReservationById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetInventoryReservationByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReservation(CreateInventoryReservationCommand command, CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetReservationById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/consume")]
    public async Task<IActionResult> ConsumeReservation(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ConsumeInventoryReservationCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/release")]
    public async Task<IActionResult> ReleaseReservation(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ReleaseInventoryReservationCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/expire")]
    public async Task<IActionResult> ExpireReservation(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ExpireInventoryReservationCommand(id), cancellationToken);
        return NoContent();
    }
}
