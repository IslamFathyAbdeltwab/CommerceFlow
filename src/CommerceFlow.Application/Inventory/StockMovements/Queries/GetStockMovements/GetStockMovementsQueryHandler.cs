using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Application.Inventory.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Inventory.StockMovements.Queries.GetStockMovements;

public sealed class GetStockMovementsQueryHandler(
    IStockMovementRepository movements) : IRequestHandler<GetStockMovementsQuery, PagedResult<StockMovementListItemDto>>
{
    public async Task<PagedResult<StockMovementListItemDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await movements.CountAsync(
            request.WarehouseId,
            request.ProductVariantId,
            request.MovementType,
            request.ReferenceId,
            cancellationToken);

        var list = await movements.GetAllAsync(
            request.WarehouseId,
            request.ProductVariantId,
            request.MovementType,
            request.ReferenceId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var result = list.Select(m => new StockMovementListItemDto(
            m.Id,
            m.WarehouseId,
            m.ProductVariantId,
            m.MovementType,
            m.QuantityChange,
            m.CreatedAtUtc)).ToList();

        return new PagedResult<StockMovementListItemDto>(result, totalCount, request.PageNumber, request.PageSize);
    }
}
