using CommerceFlow.Application.Orders.Commands.AddOrderLine;
using CommerceFlow.Application.Orders.Commands.ApplyDiscount;
using CommerceFlow.Application.Orders.Commands.CancelOrder;
using CommerceFlow.Application.Orders.Commands.ConfirmOrder;
using CommerceFlow.Application.Orders.Commands.DeliverOrder;
using CommerceFlow.Application.Orders.Commands.MarkOrderAsPaid;
using CommerceFlow.Application.Orders.Commands.PlaceOrder;
using CommerceFlow.Application.Orders.Commands.RefundOrder;
using CommerceFlow.Application.Orders.Commands.RemoveOrderLine;
using CommerceFlow.Application.Orders.Commands.SetShippingFee;
using CommerceFlow.Application.Orders.Commands.ShipOrder;
using CommerceFlow.Application.Orders.Commands.UpdateOrderLineQuantity;
using CommerceFlow.Application.Orders.Queries.GetOrderById;
using CommerceFlow.Application.Orders.Queries.GetOrders;
using CommerceFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

// Thin HTTP adapter: binds request -> Command/Query, returns the result.
// No business rules, no EF, no mapping beyond what model binding needs.
[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] Guid? customerId = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] OrderPaymentStatus? paymentStatus = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetOrdersQuery(customerId, status, paymentStatus, searchTerm, pageNumber, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetOrderByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(PlaceOrderCommand command, CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetOrderById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<IActionResult> AddLine(Guid id, AddOrderLineCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { OrderId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<IActionResult> UpdateLine(
        Guid id,
        Guid lineId,
        UpdateOrderLineQuantityCommand command,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { OrderId = id, OrderLineId = lineId }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<IActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken = default)
    {
        await sender.Send(new RemoveOrderLineCommand(id, lineId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/shipping-fee")]
    public async Task<IActionResult> SetShippingFee(Guid id, SetShippingFeeCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { OrderId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/discount")]
    public async Task<IActionResult> ApplyDiscount(Guid id, ApplyDiscountCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { OrderId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ConfirmOrderCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> MarkAsPaid(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new MarkOrderAsPaidCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/ship")]
    public async Task<IActionResult> Ship(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ShipOrderCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deliver")]
    public async Task<IActionResult> Deliver(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new DeliverOrderCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancelOrderCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { OrderId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new RefundOrderCommand(id), cancellationToken);
        return NoContent();
    }
}
