using CommerceFlow.Domain.Entities.Reviews;
using FluentValidation;

namespace CommerceFlow.Application.Reviews.Commands.CreateReview;

public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();

        // shape only: rating range, lengths and empty ids. Product existence,
        // the verified-purchase link and the duplicate rule are business
        // logic and live in the handler.
        RuleFor(x => x.Rating)
            .InclusiveBetween(ProductReview.RatingMin, ProductReview.RatingMax);
        RuleFor(x => x.Comment)
            .NotEmpty()
            .MaximumLength(ProductReview.CommentMaxLength);
        RuleFor(x => x.Title)
            .MaximumLength(ProductReview.TitleMaxLength);

        RuleFor(x => x.OrderId)
            .Must(id => id is null || id != Guid.Empty)
            .When(x => x.OrderId.HasValue)
            .WithMessage("Order ID cannot be empty when provided.");
    }
}
