using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Application.Features.Sla;

public sealed class SlaMonitoringService : ISlaMonitoringService
{
    private readonly ITicketRepository _tickets;
    private readonly ICatalogRepository _catalog;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public SlaMonitoringService(
        ITicketRepository tickets,
        ICatalogRepository catalog,
        IUserRepository users,
        IUnitOfWork unitOfWork)
    {
        _tickets = tickets;
        _catalog = catalog;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SlaExpiringNotification>> GetTicketsPendingExpiringNotificationAsync(
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Ticket> tickets = await _tickets.GetSlaTrackedTicketsAsync(cancellationToken);

        List<SlaExpiringNotification> notifications = [];

        foreach (Ticket ticket in tickets)
        {
            TicketSlaRecord? current = GetCurrentRecord(ticket);

            if (current is null
                || current.BreachedAtUtc is not null
                || current.ExpiringNotifiedAtUtc is not null
                || !SlaPolicy.IsExpiring(ticket, current, utcNow))
            {
                continue;
            }

            SlaExpiringNotification? notification = await BuildExpiringNotificationAsync(ticket, cancellationToken);

            if (notification is not null)
            {
                notifications.Add(notification);
            }
        }

        return notifications;
    }

    public async Task MarkExpiringNotifiedAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken)
    {
        if (ticketIds.Count == 0)
        {
            return;
        }

        IReadOnlyList<TicketSlaRecord> records = await _tickets.GetSlaRecordsByTicketIdsAsync(ticketIds, cancellationToken);

        DateTime now = DateTime.UtcNow;

        foreach (TicketSlaRecord record in records.Where(r => r.IsCurrent))
        {
            record.ExpiringNotifiedAtUtc = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SlaBreachedNotification>> GetTicketsPendingBreachNotificationAsync(
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Ticket> tickets = await _tickets.GetSlaTrackedTicketsAsync(cancellationToken);

        List<SlaBreachedNotification> notifications = [];
        bool breachMarked = false;

        foreach (Ticket ticket in tickets)
        {
            TicketSlaRecord? current = GetCurrentRecord(ticket);

            if (current is null || current.CanceledAtUtc is not null)
            {
                continue;
            }

            if (current.BreachedAtUtc is null && SlaPolicy.IsBreached(current, utcNow))
            {
                SlaPolicy.MarkBreached(current, utcNow);
                breachMarked = true;
            }

            if (current.BreachedAtUtc is null || current.BreachedNotifiedAtUtc is not null)
            {
                continue;
            }

            SlaBreachedNotification? notification = await BuildBreachedNotificationAsync(ticket, current, cancellationToken);

            if (notification is not null)
            {
                notifications.Add(notification);
            }
        }

        if (breachMarked)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return notifications;
    }

    public async Task MarkBreachedNotifiedAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken)
    {
        if (ticketIds.Count == 0)
        {
            return;
        }

        IReadOnlyList<TicketSlaRecord> records = await _tickets.GetSlaRecordsByTicketIdsAsync(ticketIds, cancellationToken);

        DateTime now = DateTime.UtcNow;

        foreach (TicketSlaRecord record in records.Where(r => r.IsCurrent))
        {
            record.BreachedNotifiedAtUtc = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplySlaGraceExpirationsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Ticket> tickets = await _tickets.GetSlaTrackedTicketsAsync(cancellationToken);

        bool changed = false;

        foreach (Ticket ticket in tickets)
        {
            TicketSlaRecord? current = GetCurrentRecord(ticket);

            if (current is null || current.CanceledAtUtc is not null)
            {
                continue;
            }

            if (!SlaPolicy.IsGraceExceeded(current, utcNow))
            {
                continue;
            }

            SlaPolicy.MarkCanceled(current, utcNow);

            ticket.AssignedToId = null;

            Guid? nuevoStatusId = await _catalog.FindStatusByNameAsync(
                ticket.CompanyId,
                "Nuevo",
                cancellationToken);

            if (nuevoStatusId is null)
            {
                throw new InvalidOperationException(
                    $"No se encontró el estado 'Nuevo' para la empresa {ticket.CompanyId}.");
            }

            ticket.StatusId = nuevoStatusId.Value;

            _tickets.AddComment(new TicketComment
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                AuthorId = null,
                Body = "El SLA fue cancelado por incumplimiento y vencimiento de la hora extra. " +
                       "El técnico fue desasignado y el ticket queda pendiente de nueva asignación.",
                IsInternal = true
            });

            changed = true;
        }

        if (changed)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<SlaCanceledNotification>> GetPendingSlaCanceledNotificationsAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TicketSlaRecord> records = await _tickets.GetCanceledSlaRecordsPendingNotificationAsync(cancellationToken);

        List<SlaCanceledNotification> notifications = [];

        foreach (TicketSlaRecord record in records)
        {
            SlaCanceledNotification? notification = await BuildCanceledNotificationAsync(record, cancellationToken);

            if (notification is not null)
            {
                notifications.Add(notification);
            }
        }

        return notifications;
    }

    public async Task MarkSlaCanceledNotifiedAsync(
        IReadOnlyCollection<Guid> recordIds,
        CancellationToken cancellationToken)
    {
        if (recordIds.Count == 0)
        {
            return;
        }

        IReadOnlyList<TicketSlaRecord> records = await _tickets.GetSlaRecordsByIdsAsync(recordIds, cancellationToken);

        DateTime now = DateTime.UtcNow;

        foreach (TicketSlaRecord record in records)
        {
            record.CanceledNotifiedAtUtc = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<SlaExpiringNotification?> BuildExpiringNotificationAsync(
        Ticket ticket,
        CancellationToken cancellationToken)
    {
        TicketNotificationInfo? info = await _tickets.GetTicketNotificationInfoAsync(ticket.Id, cancellationToken);

        if (info is null)
        {
            return null;
        }

        return new SlaExpiringNotification
        {
            TicketId = ticket.Id,
            Title = info.Title,
            PriorityName = info.PriorityName,
            TechnicianFirstName = info.AssignedToFirstName,
            TechnicianEmail = info.AssignedToEmail,
            ResponseDeadlineAtUtc = ticket.ResponseDeadlineAtUtc
        };
    }

    private async Task<SlaBreachedNotification?> BuildBreachedNotificationAsync(
        Ticket ticket,
        TicketSlaRecord record,
        CancellationToken cancellationToken)
    {
        TicketNotificationInfo? info = await _tickets.GetTicketNotificationInfoAsync(ticket.Id, cancellationToken);

        if (info is null || record.GraceDeadlineUtc is null)
        {
            return null;
        }

        return new SlaBreachedNotification
        {
            TicketId = ticket.Id,
            Title = info.Title,
            PriorityName = info.PriorityName,
            TechnicianFirstName = info.AssignedToFirstName,
            TechnicianEmail = info.AssignedToEmail,
            ResponseDeadlineAtUtc = ticket.ResponseDeadlineAtUtc,
            GraceDeadlineUtc = record.GraceDeadlineUtc.Value
        };
    }

    private async Task<SlaCanceledNotification?> BuildCanceledNotificationAsync(
        TicketSlaRecord record,
        CancellationToken cancellationToken)
    {
        if (record.TechnicianId is null || record.CanceledAtUtc is null || record.Ticket is null)
        {
            return null;
        }

        ApplicationUser? technician = await _users.GetByIdAsync(record.TechnicianId.Value, cancellationToken);

        if (technician is null)
        {
            return null;
        }

        return new SlaCanceledNotification
        {
            RecordId = record.Id,
            TicketId = record.TicketId,
            TechnicianId = technician.Id,
            Title = record.Ticket.Title,
            PriorityName = record.Priority.ToString(),
            TechnicianFirstName = technician.FirstName,
            TechnicianEmail = technician.Email ?? string.Empty,
            CanceledAtUtc = record.CanceledAtUtc.Value
        };
    }

    private static TicketSlaRecord? GetCurrentRecord(Ticket ticket) =>
        ticket.SlaRecords.SingleOrDefault(r => r.IsCurrent && r.CanceledAtUtc is null);
}