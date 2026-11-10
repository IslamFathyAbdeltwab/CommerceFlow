using System;
using System.Collections.Generic;
using System.Text;

namespace CommerceFlow.Domain.Enums
{
    // Payment lifecycle of an order - independent from fulfillment.
    // Unpaid -> Paid -> Refunded (a refund never rewrites fulfillment state).
    public enum OrderPaymentStatus
    {
        Unpaid,
        Paid,
        Refunded
    }
}
