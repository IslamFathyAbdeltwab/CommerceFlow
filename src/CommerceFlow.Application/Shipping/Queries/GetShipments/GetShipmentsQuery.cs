using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Shipping.Queries.GetShipments;

public sealed record GetShipmentsQuery(
    Guid? OrderId = null,
    ShipmentStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<ShipmentDto>>;
