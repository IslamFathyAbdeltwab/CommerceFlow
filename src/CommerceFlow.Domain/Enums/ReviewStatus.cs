namespace CommerceFlow.Domain.Enums;

// Moderation state of a product review. New reviews start Pending; a moderator
// approves or rejects them, and approved content can be taken down later
// (abuse/policy). Rejected is terminal - the customer appeals by submitting a
// new review, so published content never re-enters moderation silently.
public enum ReviewStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}
