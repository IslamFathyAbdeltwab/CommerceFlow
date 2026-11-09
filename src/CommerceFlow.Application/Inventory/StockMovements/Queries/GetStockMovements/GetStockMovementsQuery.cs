using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Inventory.StockMovements.Queries.GetStockMovements;

public sealed record GetStockMovementsQuery(
    Guid? WarehouseId = null,
    Guid? ProductVariantId = null,
    StockMovementType? MovementType = null,
    Guid? ReferenceId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<StockMovementListItemDto>>;
