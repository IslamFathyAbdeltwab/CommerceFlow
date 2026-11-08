using CommerceFlow.Application.Catalog.Categories.Commands.ActivateCategory;
using CommerceFlow.Application.Catalog.Categories.Commands.CreateCategory;
using CommerceFlow.Application.Catalog.Categories.Commands.DeactivateCategory;
using CommerceFlow.Application.Catalog.Categories.Commands.UpdateCategory;
using CommerceFlow.Application.Catalog.Categories.Queries.GetCategories;
using CommerceFlow.Application.Catalog.Categories.Queries.GetCategoryById;
using CommerceFlow.Application.Catalog.Categories.Queries.GetCategoryTree;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCategories(
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCategoriesQuery(searchTerm, isActive, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetCategoryTree(
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCategoryTreeQuery(onlyActive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCategoryByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory(CreateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetCategoryById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, UpdateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { CategoryId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateCategory(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ActivateCategoryCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateCategory(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new DeactivateCategoryCommand(id), cancellationToken);
        return NoContent();
    }
}
