using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Application.Features.Chat;

public sealed class ChatService : IChatService
{
    private readonly IChatMessageRepository _chatMessages;
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    private const int HistoryLimit = 100;

    public ChatService(
        IChatMessageRepository chatMessages,
        ITicketRepository tickets,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _chatMessages = chatMessages;
        _tickets = tickets;
        _users = users;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        bool hasAccess = await CanAccessTicketChatAsync(ticketId, cancellationToken);

        if (!hasAccess)
        {
            throw new UnauthorizedException("No tenés acceso al chat de este ticket.");
        }

        return await _chatMessages.GetByTicketIdAsync(ticketId, HistoryLimit, cancellationToken);
    }

    public async Task<ChatMessageDto> SaveMessageAsync(
        Guid ticketId,
        string content,
        CancellationToken cancellationToken)
    {
        bool hasAccess = await CanAccessTicketChatAsync(ticketId, cancellationToken);

        if (!hasAccess)
        {
            throw new UnauthorizedException("No tenés acceso al chat de este ticket.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Content"] = ["El mensaje no puede estar vacío."]
            });
        }

        ChatMessage message = new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            SenderId = _currentUser.UserId,
            Content = content.Trim(),
            SentAtUtc = DateTime.UtcNow
        };

        _chatMessages.Add(message);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        ApplicationUser? sender = await _users.GetByIdAsync(_currentUser.UserId, cancellationToken);

        return new ChatMessageDto
        {
            Id = message.Id,
            TicketId = message.TicketId,
            SenderId = message.SenderId,
            SenderFirstName = sender?.FirstName ?? string.Empty,
            SenderLastName = sender?.LastName ?? string.Empty,
            Content = message.Content,
            SentAtUtc = message.SentAtUtc
        };
    }

    public async Task<bool> CanAccessTicketChatAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        Ticket? ticket = await _tickets.GetByIdAsync(ticketId, _currentUser.CompanyId, cancellationToken);

        if (ticket is null)
        {
            return false;
        }

        Guid userId = _currentUser.UserId;

        return ticket.CreatedById == userId || ticket.AssignedToId == userId;
    }
}
