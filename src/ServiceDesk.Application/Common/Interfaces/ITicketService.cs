using ServiceDesk.Application.DTOs.Tickets;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ITicketService
{
    Task<TicketDto> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketDto>> GetMineAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TicketDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TechnicianDto>> GetTechniciansAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketDto>> GetAssignedToMeAsync(CancellationToken cancellationToken = default);

    Task<TicketDto> ResolveAsync(
        Guid id,
        ResolveTicketRequest request,
        CancellationToken cancellationToken = default);

    Task<TicketDto> StartWorkAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TicketDto> UpdateAsync(
        Guid id,
        UpdateTicketRequest request,
        CancellationToken cancellationToken = default);

    Task<TicketDto> AssignAsync(
        Guid id,
        AssignTicketRequest request,
        CancellationToken cancellationToken = default);

    Task<AttachmentDownloadResult> DownloadAttachmentAsync(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);
}
