using CommerceFlow.Application.Inventory.StockMovements.Queries.GetStockMovements;
using CommerceFlow.Application.Inventory.StockMovements.Queries.GetStockMovementById;
using CommerceFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

// StockMovements are append-only: reads only here.
// Writes (receipts, shipments, adjustments) happen via InventoryItems commands
// which write movements in the same SaveChanges transaction.
[ApiController]
[Route("api/[controller]")]
public sealed class StockMovementsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetStockMovements(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? productVariantId = null,
        [FromQuery] StockMovementType? movementType = null,
        [FromQuery] Guid? referenceId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetStockMovementsQuery(warehouseId, productVariantId, movementType, referenceId, pageNumber, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStockMovementById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetStockMovementByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
