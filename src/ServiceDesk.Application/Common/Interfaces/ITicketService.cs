using ServiceDesk.Application.DTOs.Tickets;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ITicketService
{
    Task<TicketDto> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketDto>> GetMineAsync(CancellationToken cancellationToken = default);
}
