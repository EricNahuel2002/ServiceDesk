using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ITicketRepository
{
    Task<IReadOnlyList<TicketDto>> GetMineAsync(Guid createdById, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketDto>> GetAssignedToAsync(Guid assignedToId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketDto>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<TicketDto?> GetDtoByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);

    Task<Ticket?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);

    Task<Ticket?> GetAssignedTicketByIdAsync(
        Guid id,
        Guid companyId,
        Guid assignedToId,
        CancellationToken cancellationToken = default);

    void Add(Ticket ticket);

    void AddComment(TicketComment comment);
}
