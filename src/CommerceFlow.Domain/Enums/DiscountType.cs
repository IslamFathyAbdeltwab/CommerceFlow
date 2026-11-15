namespace CommerceFlow.Domain.Enums;

// How a promotion cuts the price. Percentage scales with the order (always
// rounded to 2dp and capped at the order amount by Promotion.CalculateDiscount);
// FixedAmount is a flat cut that is likewise never allowed to exceed what
// the order costs.
public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1,
}
