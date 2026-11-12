using System;
using System.Collections.Generic;
using System.Text;

namespace CommerceFlow.Domain.Enums
{
    // Carrier lifecycle of one shipment. Kept independent from OrderStatus
    // (the order's coarse fulfillment view): an order becomes Shipped while
    // this record carries the finer carrier states, and partial shipments
    // mean several shipments can exist per order. Carrier webhooks arrive
    // at least once, so repeating the current state is a no-op.
    public enum ShipmentStatus
    {
        Pending,    // created, label printed, awaiting carrier pickup
        Shipped,    // handed over to the carrier
        InTransit,  // moving between carrier hubs
        Delivered,  // recipient signed for it
        Failed,     // delivery attempt failed (wrong address, nobody home)
        Returned,   // sent back to the sender after a failed delivery
        Cancelled   // stopped before hand-off
    }
}
