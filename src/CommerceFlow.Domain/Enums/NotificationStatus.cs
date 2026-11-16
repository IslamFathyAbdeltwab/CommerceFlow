namespace CommerceFlow.Domain.Enums;

// Delivery lifecycle, forward-only like the other modules: Pending is picked
// up into Queued, the transport confirms Sent, the recipient finally Reads
// it. Failed is reachable from every non-read state. The single backwards
// move is the operator retry Failed -> Queued - a new attempt, not a new
// record - while the failure trail (reason, count, timestamp) stays behind
// for diagnostics.
//
// Each status is a confirmation of an at-least-once transport: a repeated
// confirmation is ignored, never an error (see Notification.MarkQueued etc).
public enum NotificationStatus
{
    Pending = 0,
    Queued = 1,
    Sent = 2,
    Read = 3,
    Failed = 4,
}
