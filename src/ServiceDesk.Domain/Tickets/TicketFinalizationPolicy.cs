using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Domain.Tickets;

public static class TicketFinalizationPolicy
{
    public static void EnsureCanBeFinalizedBy(Ticket ticket, ApplicationUser technician)
    {
        if (technician is null || !technician.IsActive)
        {
            throw new DomainRuleViolationException("Solo un técnico activo puede finalizar un ticket.");
        }

        if (!technician.IsInRole(Roles.Tecnico))
        {
            throw new DomainRuleViolationException("Solo un usuario con el rol de técnico puede finalizar un ticket.");
        }

        if (ticket.AssignedToId != technician.Id)
        {
            throw new DomainRuleViolationException("Un ticket solo puede ser finalizado por el técnico al que fue asignado.");
        }
    }
}
