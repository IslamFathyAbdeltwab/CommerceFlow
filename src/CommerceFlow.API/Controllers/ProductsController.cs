using CommerceFlow.Application.Catalog.Products.Commands.ActivateVariant;
using CommerceFlow.Application.Catalog.Products.Commands.AddProductCategory;
using CommerceFlow.Application.Catalog.Products.Commands.AddProductImage;
using CommerceFlow.Application.Catalog.Products.Commands.AddProductVariant;
using CommerceFlow.Application.Catalog.Products.Commands.ArchiveProduct;
using CommerceFlow.Application.Catalog.Products.Commands.ChangeVariantPrice;
using CommerceFlow.Application.Catalog.Products.Commands.CreateProduct;
using CommerceFlow.Application.Catalog.Products.Commands.DeactivateVariant;
using CommerceFlow.Application.Catalog.Products.Commands.FeatureProduct;
using CommerceFlow.Application.Catalog.Products.Commands.PublishProduct;
using CommerceFlow.Application.Catalog.Products.Commands.RemoveProductCategory;
using CommerceFlow.Application.Catalog.Products.Commands.RemoveProductImage;
using CommerceFlow.Application.Catalog.Products.Commands.RemoveVariant;
using CommerceFlow.Application.Catalog.Products.Commands.RemoveVariantAttributeValue;
using CommerceFlow.Application.Catalog.Products.Commands.SetProductPrimaryImage;
using CommerceFlow.Application.Catalog.Products.Commands.SetVariantAttributeValue;
using CommerceFlow.Application.Catalog.Products.Commands.UnfeatureProduct;
using CommerceFlow.Application.Catalog.Products.Commands.UpdateProduct;
using CommerceFlow.Application.Catalog.Products.Commands.UpdateProductVariant;
using CommerceFlow.Application.Catalog.Products.Queries.GetFeaturedProducts;
using CommerceFlow.Application.Catalog.Products.Queries.GetProductById;
using CommerceFlow.Application.Catalog.Products.Queries.GetProductBySlug;
using CommerceFlow.Application.Catalog.Products.Queries.GetProducts;
using CommerceFlow.Application.Catalog.Products.Queries.GetProductStats;
using CommerceFlow.Application.Catalog.Products.Queries.GetProductVariants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    // ---------- Queries: product listing & details ----------

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetProductsQuery(searchTerm, brandId, categoryId, isFeatured, minPrice, maxPrice, pageNumber, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeaturedProducts(
        [FromQuery] int take = 8,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetFeaturedProductsQuery(take), cancellationToken);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetProductStats(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetProductStatsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetProductBySlug(string slug, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetProductBySlugQuery(slug), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/variants")]
    public async Task<IActionResult> GetProductVariants(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetProductVariantsQuery(id), cancellationToken);
        return Ok(result);
    }

    // ---------- Commands: product lifecycle ----------

    [HttpPost]
    public async Task<IActionResult> CreateProduct(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProductById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { ProductId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> PublishProduct(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new PublishProductCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> ArchiveProduct(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ArchiveProductCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/feature")]
    public async Task<IActionResult> FeatureProduct(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new FeatureProductCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/unfeature")]
    public async Task<IActionResult> UnfeatureProduct(Guid id, CancellationToken cancellationToken = default)
    {
        await sender.Send(new UnfeatureProductCommand(id), cancellationToken);
        return NoContent();
    }

    // ---------- Commands: variants ----------

    [HttpPost("{id:guid}/variants")]
    public async Task<IActionResult> AddProductVariant(Guid id, AddProductVariantCommand command, CancellationToken cancellationToken = default)
    {
        var variantId = await sender.Send(command with { ProductId = id }, cancellationToken);
        return Created($"/api/products/{id}/variants/{variantId}", new { id = variantId });
    }

    [HttpPut("{id:guid}/variants/{variantId:guid}")]
    public async Task<IActionResult> UpdateProductVariant(
        Guid id,
        Guid variantId,
        UpdateProductVariantCommand command,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { ProductId = id, VariantId = variantId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/variants/{variantId:guid}/price")]
    public async Task<IActionResult> ChangeVariantPrice(
        Guid id,
        Guid variantId,
        ChangeVariantPriceCommand command,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(command with { ProductId = id, VariantId = variantId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/variants/{variantId:guid}/activate")]
    public async Task<IActionResult> ActivateVariant(Guid id, Guid variantId, CancellationToken cancellationToken = default)
    {
        await sender.Send(new ActivateVariantCommand(id, variantId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/variants/{variantId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateVariant(Guid id, Guid variantId, CancellationToken cancellationToken = default)
    {
        await sender.Send(new DeactivateVariantCommand(id, variantId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/variants/{variantId:guid}")]
    public async Task<IActionResult> RemoveVariant(Guid id, Guid variantId, CancellationToken cancellationToken = default)
    {
        await sender.Send(new RemoveVariantCommand(id, variantId), cancellationToken);
        return NoContent();
    }

    // ---------- Commands: images ----------

    [HttpPost("{id:guid}/images")]
    public async Task<IActionResult> AddProductImage(Guid id, AddProductImageCommand command, CancellationToken cancellationToken = default)
    {
        var imageId = await sender.Send(command with { ProductId = id }, cancellationToken);
        return Created($"/api/products/{id}/images/{imageId}", new { id = imageId });
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> RemoveProductImage(Guid id, Guid imageId, CancellationToken cancellationToken = default)
    {
        await sender.Send(new RemoveProductImageCommand(id, imageId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/images/{imageId:guid}/make-primary")]
    public async Task<IActionResult> SetProductPrimaryImage(Guid id, Guid imageId, CancellationToken cancellationToken = default)
    {
        await sender.Send(new SetProductPrimaryImageCommand(id, imageId), cancellationToken);
        return NoContent();
    }

    // ---------- Commands: categories ----------

    [HttpPost("{id:guid}/categories/{categoryId:guid}")]
    public async Task<IActionResult> AddProductCategory(Guid id, Guid categoryId, CancellationToken cancellationToken = default)
    {
        await sender.Send(new AddProductCategoryCommand(id, categoryId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/categories/{categoryId:guid}")]
    public async Task<IActionResult> RemoveProductCategory(Guid id, Guid categoryId, CancellationToken cancellationToken = default)
    {
        await sender.Send(new RemoveProductCategoryCommand(id, categoryId), cancellationToken);
        return NoContent();
    }

    // ---------- Commands: variant attribute values ----------

    [HttpPut("{id:guid}/variants/{variantId:guid}/attributes/{attributeId:guid}")]
    public async Task<IActionResult> SetVariantAttributeValue(
        Guid id,
        Guid variantId,
        Guid attributeId,
        [FromBody] string value,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new SetVariantAttributeValueCommand(id, variantId, attributeId, value), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/variants/{variantId:guid}/attributes/{attributeId:guid}")]
    public async Task<IActionResult> RemoveVariantAttributeValue(
        Guid id,
        Guid variantId,
        Guid attributeId,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new RemoveVariantAttributeValueCommand(id, variantId, attributeId), cancellationToken);
        return NoContent();
    }
}
