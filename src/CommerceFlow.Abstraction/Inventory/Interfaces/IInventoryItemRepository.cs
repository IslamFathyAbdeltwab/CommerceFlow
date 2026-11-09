using CommerceFlow.Domain.Entities.Inventory;

namespace CommerceFlow.Application.Inventory.Interfaces;

public interface IInventoryItemRepository
{
    Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InventoryItem?> GetByWarehouseAndVariantAsync(Guid warehouseId, Guid productVariantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryItem>> GetAllAsync(
        Guid? warehouseId,
        Guid? productVariantId,
        bool? lowStockOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid? warehouseId,
        Guid? productVariantId,
        bool? lowStockOnly,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid warehouseId, Guid productVariantId, CancellationToken cancellationToken = default);

    Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default);
}
