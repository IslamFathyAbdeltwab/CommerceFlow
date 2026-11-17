using MediatR;

namespace CommerceFlow.Application.Orders.Events;

// In-process integration event: the Orders module announces a state change
// and stays unaware of who listens - any subscriber in the process may react
// without Orders referencing its types. Published inside the confirming
// request, BEFORE SaveChanges: subscribers write through the same unit of
// work, so the order and everything a subscriber creates commit together,
// and a subscriber that throws moves nothing (the house rule of failing
// before state moves, applied across module boundaries).
//
// Cross-process evolution (documented, not built): when the modules split
// into services this becomes a real integration event on a bus, published
// through an outbox after commit.
public sealed record OrderConfirmedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    string OrderNumber,
    decimal GrandTotal,
    string Currency) : INotification;
