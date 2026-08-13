using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.UnitTests;

public class TicketFinalizationPolicyTests
{
    [Fact]
    public void EnsureCanBeFinalizedBy_ActiveAssignedTechnician_Passes()
    {
        ApplicationUser technician = CreateUser(active: true, roles: Roles.Tecnico);
        Ticket ticket = CreateTicket(assignedToId: technician.Id);

        TicketFinalizationPolicy.EnsureCanBeFinalizedBy(ticket, technician);
    }

    [Fact]
    public void EnsureCanBeFinalizedBy_UserWithoutTechnicianRole_Throws()
    {
        ApplicationUser user = CreateUser(active: true, roles: Roles.Cliente);
        Ticket ticket = CreateTicket(assignedToId: user.Id);

        DomainRuleViolationException exception = Assert.Throws<DomainRuleViolationException>(
            () => TicketFinalizationPolicy.EnsureCanBeFinalizedBy(ticket, user));

        Assert.Contains("rol de técnico", exception.Message);
    }

    [Fact]
    public void EnsureCanBeFinalizedBy_InactiveTechnician_Throws()
    {
        ApplicationUser technician = CreateUser(active: false, roles: Roles.Tecnico);
        Ticket ticket = CreateTicket(assignedToId: technician.Id);

        DomainRuleViolationException exception = Assert.Throws<DomainRuleViolationException>(
            () => TicketFinalizationPolicy.EnsureCanBeFinalizedBy(ticket, technician));

        Assert.Contains("técnico activo", exception.Message);
    }

    [Fact]
    public void EnsureCanBeFinalizedBy_TechnicianNotAssignedToTicket_Throws()
    {
        ApplicationUser technician = CreateUser(active: true, roles: Roles.Tecnico);
        Ticket ticket = CreateTicket(assignedToId: Guid.NewGuid());

        DomainRuleViolationException exception = Assert.Throws<DomainRuleViolationException>(
            () => TicketFinalizationPolicy.EnsureCanBeFinalizedBy(ticket, technician));

        Assert.Contains("técnico al que fue asignado", exception.Message);
    }

    private static ApplicationUser CreateUser(bool active, params string[] roles)
    {
        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            IsActive = active,
            Roles = roles
                .Select(name => new ApplicationRole(name))
                .ToList()
        };

        return user;
    }

    private static Ticket CreateTicket(Guid assignedToId) =>
        new()
        {
            Id = Guid.NewGuid(),
            AssignedToId = assignedToId
        };
}
