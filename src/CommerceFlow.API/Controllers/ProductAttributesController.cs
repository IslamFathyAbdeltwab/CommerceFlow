using CommerceFlow.Application.Catalog.ProductAttributes.Commands.CreateProductAttribute;
using CommerceFlow.Application.Catalog.ProductAttributes.Commands.UpdateProductAttribute;
using CommerceFlow.Application.Catalog.ProductAttributes.Queries.GetProductAttributeById;
using CommerceFlow.Application.Catalog.ProductAttributes.Queries.GetProductAttributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductAttributesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProductAttributes(
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isVariantDefining = null,
        [FromQuery] bool? isFilterable = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetProductAttributesQuery(searchTerm, isVariantDefining, isFilterable, pageNumber, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProductAttributeById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetProductAttributeByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProductAttribute(
        CreateProductAttributeCommand command,
        CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProductAttributeById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProductAttribute(
        Guid id,
        UpdateProductAttributeCommand command,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { AttributeId = id }, cancellationToken);
        return NoContent();
    }
}
