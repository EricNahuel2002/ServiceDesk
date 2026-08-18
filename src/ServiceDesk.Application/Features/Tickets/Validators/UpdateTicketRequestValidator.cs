using FluentValidation;
using ServiceDesk.Application.DTOs.Tickets;

namespace ServiceDesk.Application.Features.Tickets.Validators;

public sealed class UpdateTicketRequestValidator : AbstractValidator<UpdateTicketRequest>
{
    public UpdateTicketRequestValidator()
    {
        RuleFor(x => x.AssignedToId)
            .NotEmpty()
            .WithMessage("El técnico asignado es obligatorio.");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("La prioridad indicada no es válida.");

        RuleFor(x => x.StatusId)
            .NotEmpty()
            .WithMessage("El estado es obligatorio.");
    }
}
