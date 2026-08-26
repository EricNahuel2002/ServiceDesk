using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Audits;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Audit;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Application.Features.Audits;

public sealed class AuditService : IAuditService
{
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly IIdentityService _identity;
    private readonly ICurrentUserService _currentUser;
    private readonly IChatMessageRepository _chatMessages;

    private const int ChatHistoryLimit = 200;

    public AuditService(
        ITicketRepository tickets,
        IUserRepository users,
        IIdentityService identity,
        ICurrentUserService currentUser,
        IChatMessageRepository chatMessages)
    {
        _tickets = tickets;
        _users = users;
        _identity = identity;
        _currentUser = currentUser;
        _chatMessages = chatMessages;
    }

    public async Task<IReadOnlyList<TechnicianDto>> GetTechniciansAsync(
        CancellationToken cancellationToken) =>
        await _users.GetTechniciansAsync(_currentUser.CompanyId, cancellationToken);

    public async Task<IReadOnlyList<TicketDto>> GetTechnicianTicketsAsync(
        Guid technicianId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyTechnicianAsync(technicianId, cancellationToken);

        return await _tickets.GetAssignedToInCompanyAsync(
            technicianId,
            _currentUser.CompanyId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<TicketAuditEventDto>> GetTicketHistoryAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyTicketExistsAsync(ticketId, cancellationToken);

        IReadOnlyList<AuditLog> logs = await _tickets.GetTicketAuditLogsAsync(
            ticketId,
            _currentUser.CompanyId,
            cancellationToken);

        return logs.Select(MapEvent).ToList();
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetTicketChatAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyTicketExistsAsync(ticketId, cancellationToken);

        IReadOnlyList<ChatMessageDto> messages = await _chatMessages.GetByTicketIdAsync(
            ticketId,
            ChatHistoryLimit,
            cancellationToken);

        return messages.OrderBy(m => m.SentAtUtc).ToList();
    }

    private static TicketAuditEventDto MapEvent(AuditLog log) => new()
    {
        OccurredAtUtc = log.CreatedAtUtc,
        Action = log.Action,
        Description = log.Description,
        Details = log.Details,
        ActorName = FormatActorName(log.User?.FirstName, log.User?.LastName)
    };

    private async Task EnsureCompanyTechnicianAsync(Guid technicianId, CancellationToken cancellationToken)
    {
        ApplicationUser? technician = await _users.GetByIdAsync(technicianId, cancellationToken);

        if (technician is null || technician.CompanyId != _currentUser.CompanyId)
        {
            throw new NotFoundException($"El usuario con id {technicianId} no existe.");
        }

        if (!await _identity.IsInRoleAsync(technician, Roles.Tecnico, cancellationToken))
        {
            throw new NotFoundException($"El usuario con id {technicianId} no tiene el rol de técnico.");
        }
    }

    private async Task EnsureCompanyTicketExistsAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        Domain.Tickets.Ticket? ticket = await _tickets.GetByIdAsync(
            ticketId,
            _currentUser.CompanyId,
            cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"El ticket con id {ticketId} no existe.");
        }
    }

    private static string FormatActorName(string? firstName, string? lastName)
    {
        string first = firstName ?? string.Empty;
        string last = lastName ?? string.Empty;

        return $"{first} {last}".Trim();
    }
}
