using CommerceFlow.Application.Wishlists.Commands.AddWishlistItem;
using CommerceFlow.Application.Wishlists.Commands.ClearWishlist;
using CommerceFlow.Application.Wishlists.Commands.CreateWishlist;
using CommerceFlow.Application.Wishlists.Commands.RemoveWishlistItem;
using CommerceFlow.Application.Wishlists.Queries.GetWishlistByCustomerId;
using CommerceFlow.Application.Wishlists.Queries.GetWishlistById;
using CommerceFlow.Application.Wishlists.Queries.GetWishlists;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

// Thin HTTP adapter: binds request -> Command/Query, returns the result.
// No business rules, no EF, no mapping beyond what model binding needs.
[ApiController]
[Route("api/[controller]")]
public sealed class WishlistsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetWishlists(
        [FromQuery] Guid? customerId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetWishlistsQuery(customerId, pageNumber, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetWishlistById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetWishlistByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetWishlistByCustomer(Guid customerId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetWishlistByCustomerIdQuery(customerId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWishlist(CreateWishlistCommand command, CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetWishlistById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItem(Guid id, AddWishlistItemCommand command, CancellationToken cancellationToken = default)
    {
        // Returns the item id: saving an already-saved variant returns the same id (idempotent).
        var itemId = await sender.Send(command with { WishlistId = id }, cancellationToken);
        return CreatedAtAction(nameof(GetWishlistById), new { id }, new { itemId });
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId, CancellationToken cancellationToken = default)
    {
        await sender.Send(new RemoveWishlistItemCommand(id, itemId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")] // clears the items; the wishlist itself stays
    public async Task<IActionResult> ClearWishlist(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ClearWishlistCommand(id), cancellationToken);
        return NoContent();
    }
}
