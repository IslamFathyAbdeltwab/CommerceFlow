using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Promotions;

// Aggregate root: a discount campaign (coupon code) customers redeem against
// orders. The Code is only shape-checked here - uniqueness lives in the
// Application layer (pre-check) and a database unique index (backstop), the
// same split Review uses for its (product, customer) pair. Money never moves
// from this module: an order's discount is applied by Cart/Order through the
// Application layer and each redemption is recorded here as an audit row
// (PromotionUsage), which is also what the per-customer limit counts.
//
// The status machine is forward-only like ReturnRequest's: Draft is
// published into Active, Active can be paused and resumed, and every state
// can be force-ended into the terminal Expired. Redemption additionally
// depends on the clock (window + limits), so time-sensitive methods accept
// an injectable `now` - defaults to UtcNow, tests pass a fixed instant.
public class Promotion : AggregateRoot
{
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 150;
    public const int DescriptionMaxLength = 500;

    // Forward-only transition table. Paused is a reversible hold (the
    // merchant resumes by activating again); Expired is terminal - a
    // merchant wanting another campaign afterwards creates a fresh one.
    private static readonly IReadOnlyDictionary<PromotionStatus, PromotionStatus[]> AllowedTransitions =
        new Dictionary<PromotionStatus, PromotionStatus[]>
        {
            [PromotionStatus.Draft]   = [PromotionStatus.Active, PromotionStatus.Expired],
            [PromotionStatus.Active]  = [PromotionStatus.Paused, PromotionStatus.Expired],
            [PromotionStatus.Paused]  = [PromotionStatus.Active, PromotionStatus.Expired],
            [PromotionStatus.Expired] = [],
        };

    private readonly List<PromotionUsage> _usages = [];

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    // Immutable after creation: orders and receipts may already reference the
    // deal, so a campaign's economics never change - only text, window and
    // limits can be tuned while still Draft or Paused (UpdateDetails).
    public DiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }

    public PromotionStatus Status { get; private set; }

    // Redemption window [StartAtUtc, EndAtUtc) - the end is exclusive, so a
    // promotion is redeemable up to (not including) EndAtUtc. Null EndAtUtc
    // means no scheduled end; the merchant ends it explicitly (Expire).
    public DateTimeOffset StartAtUtc { get; private set; }
    public DateTimeOffset? EndAtUtc { get; private set; }

    // Optional campaign bounds - null means unbounded.
    public decimal? MinOrderAmount { get; private set; }
    public int? MaxUsesTotal { get; private set; }
    public int? MaxUsesPerCustomer { get; private set; }

    // Total redemptions as a real column so limits are queryable without
    // loading every usage row; the per-customer limit is counted from Usages
    // (both are only ever mutated together, in RecordUsage).
    public int CurrentUses { get; private set; }

    public DateTimeOffset? ActivatedAtUtc { get; private set; }
    public DateTimeOffset? PausedAtUtc { get; private set; }
    public DateTimeOffset? ExpiredAtUtc { get; private set; }

    public IReadOnlyCollection<PromotionUsage> Usages => _usages.AsReadOnly();

    private Promotion() { } // required by EF Core

    public static Promotion Create(
        string code,
        string name,
        DiscountType discountType,
        decimal discountValue,
        DateTimeOffset startAtUtc,
        string? description = null,
        DateTimeOffset? endAtUtc = null,
        decimal? minOrderAmount = null,
        int? maxUsesTotal = null,
        int? maxUsesPerCustomer = null)
    {
        var c = Required(code, "Promotion code", CodeMaxLength);
        var (n, d, s, e, min, total, per) = ValidateDetails(
            name, description, startAtUtc, endAtUtc,
            minOrderAmount, maxUsesTotal, maxUsesPerCustomer);
        var (t, v) = ValidateDiscount(discountType, discountValue);

        return new Promotion
        {
            Code = c,
            Name = n,
            Description = d,
            DiscountType = t,
            DiscountValue = v,
            Status = PromotionStatus.Draft,
            StartAtUtc = s,
            EndAtUtc = e,
            MinOrderAmount = min,
            MaxUsesTotal = total,
            MaxUsesPerCustomer = per,
            CurrentUses = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    // Marketing text, window and limits are tunable before publishing or
    // while paused; code and discount stay frozen (see DiscountType note).
    public void UpdateDetails(
        string name,
        string? description,
        DateTimeOffset startAtUtc,
        DateTimeOffset? endAtUtc,
        decimal? minOrderAmount,
        int? maxUsesTotal,
        int? maxUsesPerCustomer)
    {
        if (Status is not (PromotionStatus.Draft or PromotionStatus.Paused))
            throw new DomainException(
                "Promotion details can only be updated while the promotion is draft or paused.");

        var (n, d, s, e, min, total, per) = ValidateDetails(
            name, description, startAtUtc, endAtUtc,
            minOrderAmount, maxUsesTotal, maxUsesPerCustomer);

        // A paused campaign may already carry redemptions - never let the
        // ceiling drop below what has been recorded (it would make the
        // promotion permanently unredemable while still looking configurable).
        if (total is { } t && t < CurrentUses)
            throw new DomainException(
                "Total limit cannot be lower than the uses already recorded.");

        Name = n;
        Description = d;
        StartAtUtc = s;
        EndAtUtc = e;
        MinOrderAmount = min;
        MaxUsesTotal = total;
        MaxUsesPerCustomer = per;
        Touch();
    }

    // Publish the campaign, or resume a paused one. A future start date is
    // fine (the campaign simply becomes redeemable only once the window
    // opens); a window that has already ended cannot be published into.
    // State check runs before the clock check, same order as
    // ReturnRequest.MarkReceived: an expired promotion reports the
    // transition, not the calendar.
    public void Activate(DateTimeOffset? now = null)
    {
        if (Status == PromotionStatus.Active)
            return; // duplicate publish call: already there

        EnsureCanMoveTo(PromotionStatus.Active);

        var clock = now ?? DateTimeOffset.UtcNow;
        if (EndAtUtc is { } end && clock >= end)
            throw new DomainException("Promotion window has already ended.");

        Status = PromotionStatus.Active;
        ActivatedAtUtc ??= DateTimeOffset.UtcNow; // first publish only; resumes keep it
        Touch();
    }

    // Merchant takes the campaign offline temporarily (resumable via
    // Activate). Draft has never been published, so it cannot be paused.
    public void Pause()
    {
        if (Status == PromotionStatus.Paused)
            return; // duplicate pause call: already there

        MoveTo(PromotionStatus.Paused);
        PausedAtUtc = DateTimeOffset.UtcNow;
    }

    // Force-end. Available from every non-terminal state; ExpireIfDue calls
    // this when the window has passed. Terminal states allow nothing out.
    public void Expire()
    {
        if (Status == PromotionStatus.Expired)
            return; // duplicate end call: already there

        MoveTo(PromotionStatus.Expired);
        ExpiredAtUtc = DateTimeOffset.UtcNow;
    }

    // Clock-driven housekeeping: an Active/Paused campaign past its end date
    // is moved to Expired so the stored state matches reality. Redemption
    // checks the window regardless (EnsureRedeemable) - this only keeps the
    // status honest for anyone reading it.
    public void ExpireIfDue(DateTimeOffset? now = null)
    {
        var clock = now ?? DateTimeOffset.UtcNow;
        if ((Status is PromotionStatus.Active or PromotionStatus.Paused)
            && EndAtUtc is { } end && clock >= end)
        {
            Expire();
        }
    }

    // ---------- Redemption ----------

    // Would this customer be allowed to redeem the campaign on an order of
    // this size right now? Throws with the reason otherwise - the
    // Application layer pre-checks with this (409/400) before any order
    // state moves, exactly like its other state guards.
    public void EnsureCanRedeem(Guid customerId, decimal orderAmount, DateTimeOffset? now = null)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required.");

        if (orderAmount < 0)
            throw new DomainException("Order amount cannot be negative.");

        EnsureRedeemable(customerId, now ?? DateTimeOffset.UtcNow);

        if (MinOrderAmount is { } min && orderAmount < min)
            throw new DomainException(
                "Order amount does not meet the promotion's minimum order amount.");
    }

    // Record a successful redemption: appends the audit row and bumps the
    // total. Re-runs the shared redeemability core (status, window, both
    // limits) as a safety net - a caller that skipped EnsureCanRedeem still
    // cannot violate the campaign's invariants.
    public void RecordUsage(Guid customerId, Guid orderId, decimal discountTaken, DateTimeOffset? now = null)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required.");

        if (orderId == Guid.Empty)
            throw new DomainException("Order ID is required.");

        if (discountTaken <= 0)
            throw new DomainException("Discount taken must be positive.");

        // one redemption per order per campaign (the database unique index on
        // (PromotionId, OrderId) is the backstop)
        if (_usages.Any(u => u.OrderId == orderId))
            throw new DomainException("Promotion has already been redeemed on this order.");

        EnsureRedeemable(customerId, now ?? DateTimeOffset.UtcNow);

        _usages.Add(new PromotionUsage(Id, customerId, orderId, discountTaken));
        CurrentUses++;
        Touch();
    }

    // Pure calculation - no state is checked here (the caller runs
    // EnsureCanRedeem first). Percentage rounds half-away-from-zero to 2dp;
    // neither type may ever take off more than the order costs.
    public decimal CalculateDiscount(decimal orderAmount)
    {
        if (orderAmount < 0)
            throw new DomainException("Order amount cannot be negative.");

        var raw = DiscountType switch
        {
            DiscountType.Percentage  => orderAmount * DiscountValue / 100m,
            DiscountType.FixedAmount => DiscountValue,
            _ => throw new DomainException("Discount type is required."),
        };

        return Math.Min(Math.Round(raw, 2, MidpointRounding.AwayFromZero), orderAmount);
    }

    // ---------- Transition machine + validation ----------

    // Shared by EnsureCanRedeem and RecordUsage: everything that does not
    // depend on the order amount (status, window, total, per-customer).
    private void EnsureRedeemable(Guid customerId, DateTimeOffset now)
    {
        if (Status != PromotionStatus.Active)
            throw new DomainException(
                $"Promotion cannot be redeemed while its status is {Status}.");

        if (now < StartAtUtc)
            throw new DomainException("Promotion has not started yet.");

        if (EndAtUtc is { } end && now >= end) // window is [start, end)
            throw new DomainException("Promotion has ended.");

        if (MaxUsesTotal is { } total && CurrentUses >= total)
            throw new DomainException("Promotion has reached its usage limit.");

        if (MaxUsesPerCustomer is { } perCustomer &&
            _usages.Count(u => u.CustomerId == customerId) >= perCustomer)
        {
            throw new DomainException(
                "Customer has reached the usage limit for this promotion.");
        }
    }

    private void MoveTo(PromotionStatus target)
    {
        EnsureCanMoveTo(target);
        Status = target;
        Touch();
    }

    private void EnsureCanMoveTo(PromotionStatus target)
    {
        if (!AllowedTransitions[Status].Contains(target))
            throw new DomainException(
                $"Promotion cannot move from {Status} to {target}.");
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static (DiscountType Type, decimal Value) ValidateDiscount(
        DiscountType discountType,
        decimal discountValue)
    {
        if (!Enum.IsDefined(discountType))
            throw new DomainException("Discount type is required.");

        if (discountValue <= 0)
            throw new DomainException("Discount value must be positive.");

        if (discountType == DiscountType.Percentage && discountValue > 100)
            throw new DomainException("Percentage discount cannot exceed 100%.");

        return (discountType, discountValue);
    }

    // Shared by Create and UpdateDetails (everything except code/discount,
    // which are validated only once - at creation).
    private static (
        string Name,
        string? Description,
        DateTimeOffset StartAtUtc,
        DateTimeOffset? EndAtUtc,
        decimal? MinOrderAmount,
        int? MaxUsesTotal,
        int? MaxUsesPerCustomer) ValidateDetails(
        string? name,
        string? description,
        DateTimeOffset startAtUtc,
        DateTimeOffset? endAtUtc,
        decimal? minOrderAmount,
        int? maxUsesTotal,
        int? maxUsesPerCustomer)
    {
        var n = Required(name, "Promotion name", NameMaxLength);
        var d = Optional(description, "Description", DescriptionMaxLength);

        if (startAtUtc == default)
            throw new DomainException("Start date is required.");

        if (endAtUtc is { } end && end <= startAtUtc)
            throw new DomainException("End date must be after the start date.");

        if (minOrderAmount is { } min && min <= 0)
            throw new DomainException("Minimum order amount must be positive.");

        if (maxUsesTotal is { } total && total <= 0)
            throw new DomainException("Maximum uses must be positive.");

        if (maxUsesPerCustomer is { } per)
        {
            if (per <= 0)
                throw new DomainException("Per-customer usage limit must be positive.");

            if (maxUsesTotal is { } totalLimit && per > totalLimit)
                throw new DomainException(
                    "Per-customer limit cannot exceed the total limit.");
        }

        return (n, d, startAtUtc, endAtUtc, minOrderAmount, maxUsesTotal, maxUsesPerCustomer);
    }

    // Accepts `string?` so ValidateDetails can forward its nullable name
    // without a warning - IsNullOrWhiteSpace rejects null at runtime anyway.
    private static string Required(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");

        return trimmed;
    }

    // Optional text: null/whitespace stays null, anything else is trimmed
    // and length-checked (same rule as ReturnRequest.Create's notes).
    private static string? Optional(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");

        return trimmed;
    }
}
