using CommerceFlow.Application.Addresses.Commands.CreateAddress;
using CommerceFlow.Application.Addresses.Commands.DeleteAddress;
using CommerceFlow.Application.Addresses.Commands.SetDefaultAddress;
using CommerceFlow.Application.Addresses.Commands.UpdateAddress;
using CommerceFlow.Application.Addresses.Queries.GetAddressById;
using CommerceFlow.Application.Addresses.Queries.GetAddresses;
using CommerceFlow.Application.Addresses.Queries.GetDefaultAddress;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

// Thin HTTP adapter: binds request -> Command/Query, returns the result.
// No business rules, no EF, no mapping beyond what model binding needs.
[ApiController]
[Route("api/[controller]")]
public sealed class AddressesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAddresses(
        [FromQuery] Guid? customerId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetAddressesQuery(customerId, pageNumber, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAddressById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAddressByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("customer/{customerId:guid}/default")]
    public async Task<IActionResult> GetDefaultAddress(Guid customerId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetDefaultAddressQuery(customerId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAddress(CreateAddressCommand command, CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAddressById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAddress(Guid id, UpdateAddressCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { AddressId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/default")] // idempotent by design -> PUT
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new SetDefaultAddressCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new DeleteAddressCommand(id), cancellationToken);
        return NoContent();
    }
}
