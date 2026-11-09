using MediatR;

namespace CommerceFlow.Application.Inventory.StockMovements.Queries.GetStockMovementById;

public sealed record GetStockMovementByIdQuery(Guid MovementId) : IRequest<StockMovementDto?>;
