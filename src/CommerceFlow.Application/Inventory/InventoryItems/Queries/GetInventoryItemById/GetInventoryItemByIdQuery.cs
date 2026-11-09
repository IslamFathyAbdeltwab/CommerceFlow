using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Queries.GetInventoryItemById;

public sealed record GetInventoryItemByIdQuery(Guid InventoryItemId) : IRequest<InventoryItemDto?>;
