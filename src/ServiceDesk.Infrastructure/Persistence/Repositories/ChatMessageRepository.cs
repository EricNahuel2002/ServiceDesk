using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Infrastructure.Persistence.Repositories;

public sealed class ChatMessageRepository : IChatMessageRepository
{
    private readonly ServiceDeskDbContext _context;

    public ChatMessageRepository(ServiceDeskDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetByTicketIdAsync(
        Guid ticketId,
        int limit,
        CancellationToken cancellationToken = default) =>
        await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.TicketId == ticketId)
            .OrderByDescending(m => m.SentAtUtc)
            .Take(limit)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                TicketId = m.TicketId,
                SenderId = m.SenderId,
                SenderFirstName = m.Sender!.FirstName,
                SenderLastName = m.Sender.LastName,
                Content = m.Content,
                SentAtUtc = m.SentAtUtc
            })
            .ToListAsync(cancellationToken);

    public void Add(ChatMessage message) => _context.ChatMessages.Add(message);
}
