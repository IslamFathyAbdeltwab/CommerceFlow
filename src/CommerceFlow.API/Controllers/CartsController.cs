using CommerceFlow.Application.Carts.Commands.AbandonCart;
using CommerceFlow.Application.Carts.Commands.AddCartItem;
using CommerceFlow.Application.Carts.Commands.ChangeCartItemQuantity;
using CommerceFlow.Application.Carts.Commands.CheckoutCart;
using CommerceFlow.Application.Carts.Commands.ClearCart;
using CommerceFlow.Application.Carts.Commands.CreateCart;
using CommerceFlow.Application.Carts.Commands.RemoveCartItem;
using CommerceFlow.Application.Carts.Queries.GetActiveCartByCustomerId;
using CommerceFlow.Application.Carts.Queries.GetCartById;
using CommerceFlow.Application.Carts.Queries.GetCarts;
using CommerceFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

// Thin HTTP adapter: binds request -> Command/Query, returns the result.
// No business rules, no EF, no mapping beyond what model binding needs.
[ApiController]
[Route("api/[controller]")]
public sealed class CartsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCarts(
        [FromQuery] Guid? customerId = null,
        [FromQuery] CartStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetCartsQuery(customerId, status, pageNumber, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCartById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCartByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("customer/{customerId:guid}/active")]
    public async Task<IActionResult> GetActiveCart(Guid customerId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetActiveCartByCustomerIdQuery(customerId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCart(CreateCartCommand command, CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetCartById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItem(Guid id, AddCartItemCommand command, CancellationToken cancellationToken = default)
    {
        // Returns the line id: adding an existing variant merges into that line.
        var lineId = await sender.Send(command with { CartId = id }, cancellationToken);
        return CreatedAtAction(nameof(GetCartById), new { id }, new { lineId });
    }

    [HttpPut("{id:guid}/items/{lineId:guid}")]
    public async Task<IActionResult> ChangeItemQuantity(
        Guid id,
        Guid lineId,
        ChangeCartItemQuantityCommand command,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { CartId = id, CartLineId = lineId }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/items/{lineId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid lineId, CancellationToken cancellationToken = default)
    {
        await sender.Send(new RemoveCartItemCommand(id, lineId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")] // clears the items; the cart itself stays
    public async Task<IActionResult> ClearCart(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ClearCartCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/checkout")]
    public async Task<IActionResult> Checkout(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new CheckoutCartCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/abandon")]
    public async Task<IActionResult> Abandon(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new AbandonCartCommand(id), cancellationToken);
        return NoContent();
    }
}
