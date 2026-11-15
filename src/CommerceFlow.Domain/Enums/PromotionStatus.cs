namespace CommerceFlow.Domain.Enums;

// Lifecycle of a promotion campaign. Created as Draft, published into Active
// (or resumed there from Paused), and force-ended into the terminal Expired.
// Status alone never guarantees redeemability: the current time must also
// fall inside the campaign's window, which Promotion checks separately so
// the clock stays injectable and testable.
public enum PromotionStatus
{
    Draft = 0,
    Active = 1,
    Paused = 2,
    Expired = 3,
}
