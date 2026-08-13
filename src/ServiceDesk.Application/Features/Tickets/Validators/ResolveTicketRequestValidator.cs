using FluentValidation;
using ServiceDesk.Application.DTOs.Tickets;

namespace ServiceDesk.Application.Features.Tickets.Validators;

public sealed class ResolveTicketRequestValidator : AbstractValidator<ResolveTicketRequest>
{
    private const int MaxResolutionNoteLength = 2000;

    public ResolveTicketRequestValidator()
    {
        RuleFor(x => x.ResolutionNote)
            .MaximumLength(MaxResolutionNoteLength)
            .WithMessage($"La nota de resolución no puede superar los {MaxResolutionNoteLength} caracteres.");
    }
}
