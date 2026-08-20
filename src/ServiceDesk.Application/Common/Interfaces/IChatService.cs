using ServiceDesk.Application.DTOs.Chat;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IChatService
{
    Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<ChatMessageDto> SaveMessageAsync(
        Guid ticketId,
        string content,
        CancellationToken cancellationToken = default);

    Task<bool> CanAccessTicketChatAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);
}
