using FluentValidation;
using ServiceDesk.Application.DTOs.Tickets;

namespace ServiceDesk.Application.Features.Tickets.Validators;

public sealed class SubmitTicketFeedbackRequestValidator : AbstractValidator<SubmitTicketFeedbackRequest>
{
    private const int MaxCommentLength = 2000;

    private const int MinRating = 1;

    private const int MaxRating = 5;

    public SubmitTicketFeedbackRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(MinRating, MaxRating)
            .When(x => x.Rating.HasValue)
            .WithMessage($"La calificación debe estar entre {MinRating} y {MaxRating}.");

        RuleFor(x => x.Comment)
            .MaximumLength(MaxCommentLength)
            .WithMessage($"El comentario no puede superar los {MaxCommentLength} caracteres.");
    }
}
