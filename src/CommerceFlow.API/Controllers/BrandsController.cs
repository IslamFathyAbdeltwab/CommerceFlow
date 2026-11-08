using CommerceFlow.Application.Catalog.Brands.Commands.ActivateBrand;
using CommerceFlow.Application.Catalog.Brands.Commands.CreateBrand;
using CommerceFlow.Application.Catalog.Brands.Commands.DeactivateBrand;
using CommerceFlow.Application.Catalog.Brands.Commands.UpdateBrand;
using CommerceFlow.Application.Catalog.Brands.Queries.GetBrandById;
using CommerceFlow.Application.Catalog.Brands.Queries.GetBrandBySlug;
using CommerceFlow.Application.Catalog.Brands.Queries.GetBrands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;


[ApiController]
[Route("api/[controller]")]
public sealed class BrandsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBrands(
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetBrandsQuery(searchTerm, isActive, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBrandById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetBrandByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBrandBySlug(string slug, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetBrandBySlugQuery(slug), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBrand(CreateBrandCommand command, CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetBrandById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateBrand(Guid id, UpdateBrandCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { BrandId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateBrand(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ActivateBrandCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateBrand(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new DeactivateBrandCommand(id), cancellationToken);
        return NoContent();
    }
}
