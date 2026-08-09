using FluentValidation;
using ServiceDesk.Application.DTOs.Tickets;

namespace ServiceDesk.Application.Features.Tickets.Validators;

public sealed class UpdateTicketRequestValidator : AbstractValidator<UpdateTicketRequest>
{
    public UpdateTicketRequestValidator()
    {
        RuleFor(x => x.AssignedToId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.PriorityId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.StatusId)
            .NotEqual(Guid.Empty);
    }
}
