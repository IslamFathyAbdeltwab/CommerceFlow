using System;
using System.Collections.Generic;
using System.Text;

namespace CommerceFlow.Domain.Enums
{
    // Lifecycle of a single payment attempt. OrderPaymentStatus is the
    // order's coarse view (Unpaid/Paid/Refunded); this is the detailed
    // record behind it: Pending -> Succeeded -> Refunded, with Failed as
    // the rejection branch. Duplicate gateway callbacks are no-ops.
    public enum PaymentStatus
    {
        Pending,    // created, waiting for the gateway's answer
        Succeeded,  // gateway confirmed the charge
        Failed,     // gateway rejected it (declined card, etc.)
        Refunded    // money returned after a successful charge
    }
}
