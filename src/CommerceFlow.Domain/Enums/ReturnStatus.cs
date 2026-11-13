namespace CommerceFlow.Domain.Enums;

// Lifecycle of a return request (RMA). A request starts Pending, then the
// merchant approves or rejects it (or the customer withdraws it). Approved
// boxes are received by the warehouse and completed; the money itself comes
// back through Payments - this status only tracks the goods.
public enum ReturnStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Received = 3,
    Completed = 4,
    Cancelled = 5,
}
