using CommerceFlow.Application.Inventory.Warehouses.Commands.ActivateWarehouse;
using CommerceFlow.Application.Inventory.Warehouses.Commands.CreateWarehouse;
using CommerceFlow.Application.Inventory.Warehouses.Commands.DeactivateWarehouse;
using CommerceFlow.Application.Inventory.Warehouses.Commands.UpdateWarehouse;
using CommerceFlow.Application.Inventory.Warehouses.Queries.GetWarehouseById;
using CommerceFlow.Application.Inventory.Warehouses.Queries.GetWarehouses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

// Thin HTTP adapter: binds request -> Command/Query, returns the result.
// No business rules, no EF, no mapping beyond what model binding needs.
[ApiController]
[Route("api/[controller]")]
public sealed class WarehousesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetWarehouses(
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetWarehousesQuery(searchTerm, isActive, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetWarehouseById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetWarehouseByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWarehouse(CreateWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetWarehouseById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateWarehouse(Guid id, UpdateWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { WarehouseId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateWarehouse(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ActivateWarehouseCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateWarehouse(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new DeactivateWarehouseCommand(id), cancellationToken);
        return NoContent();
    }
}
