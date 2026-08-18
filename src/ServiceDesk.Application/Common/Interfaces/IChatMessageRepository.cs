using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IChatMessageRepository
{
    Task<IReadOnlyList<ChatMessageDto>> GetByTicketIdAsync(
        Guid ticketId,
        int limit,
        CancellationToken cancellationToken = default);

    void Add(ChatMessage message);
}
