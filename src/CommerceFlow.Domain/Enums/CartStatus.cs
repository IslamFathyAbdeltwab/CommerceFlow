using System;
using System.Collections.Generic;
using System.Text;

namespace CommerceFlow.Domain.Enums
{
    // Lifecycle of a shopping cart.
    // Active carts are mutable; once checked out (converted to an order) or
    // abandoned they become read-only snapshots of what the customer wanted.
    public enum CartStatus
    {
        Active,      // editable, awaiting checkout
        CheckedOut,  // converted to an order
        Abandoned    // left behind (e.g. idle too long)
    }
}
