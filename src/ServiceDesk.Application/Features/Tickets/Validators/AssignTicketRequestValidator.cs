using FluentValidation;
using ServiceDesk.Application.DTOs.Tickets;

namespace ServiceDesk.Application.Features.Tickets.Validators;

public sealed class AssignTicketRequestValidator : AbstractValidator<AssignTicketRequest>
{
    public AssignTicketRequestValidator()
    {
        RuleFor(x => x.AssignedToId)
            .NotEmpty()
            .WithMessage("El técnico asignado es obligatorio.");
    }
}
