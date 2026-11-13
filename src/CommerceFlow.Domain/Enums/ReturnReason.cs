namespace CommerceFlow.Domain.Enums;

// Why the customer is sending the goods back. Free-text detail lives in
// ReturnRequest.Notes; the enum keeps reporting uniform across returns.
public enum ReturnReason
{
    Defective = 0,
    Damaged = 1,
    WrongItem = 2,
    NotAsDescribed = 3,
    NoLongerNeeded = 4,
    Other = 5,
}
