using System;
using System.Collections.Generic;
using System.Text;

namespace CommerceFlow.Domain.Enums
{
    // Fulfillment lifecycle of an order.
    // Kept independent from OrderPaymentStatus so a cancelled-but-paid
    // order is representable without losing information.
    public enum OrderStatus
    {
        Pending,    // created, editable, awaiting confirmation
        Confirmed,  // accepted, stock reserved/consumed
        Shipped,    // handed to the courier
        Delivered,  // customer received it
        Cancelled   // stopped before shipping
    }
}
