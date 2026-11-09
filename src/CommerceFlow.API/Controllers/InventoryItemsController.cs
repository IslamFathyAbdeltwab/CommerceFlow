using CommerceFlow.Application.Inventory.InventoryItems.Commands.AdjustStock;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ConsumeReservation;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.CreateInventoryItem;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ReceiveStock;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ReleaseReservation;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ReserveStock;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.SetReorderThreshold;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ShipStock;
using CommerceFlow.Application.Inventory.InventoryItems.Queries.GetInventoryItems;
using CommerceFlow.Application.Inventory.InventoryItems.Queries.GetInventoryItemById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InventoryItemsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetInventoryItems(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? productVariantId = null,
        [FromQuery] bool? lowStockOnly = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetInventoryItemsQuery(warehouseId, productVariantId, lowStockOnly, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetInventoryItemById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetInventoryItemByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInventoryItem(CreateInventoryItemCommand command, CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetInventoryItemById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<IActionResult> ReceiveStock(Guid id, ReceiveStockCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { InventoryItemId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/ship")]
    public async Task<IActionResult> ShipStock(Guid id, ShipStockCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { InventoryItemId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/adjust")]
    public async Task<IActionResult> AdjustStock(Guid id, AdjustStockCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { InventoryItemId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reserve")]
    public async Task<IActionResult> ReserveStock(Guid id, ReserveStockCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { InventoryItemId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/release-reservation")]
    public async Task<IActionResult> ReleaseReservation(Guid id, ReleaseReservationCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { InventoryItemId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/consume-reservation")]
    public async Task<IActionResult> ConsumeReservation(Guid id, ConsumeReservationCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { InventoryItemId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/set-reorder-threshold")]
    public async Task<IActionResult> SetReorderThreshold(Guid id, SetReorderThresholdCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { InventoryItemId = id }, cancellationToken);
        return NoContent();
    }
}
