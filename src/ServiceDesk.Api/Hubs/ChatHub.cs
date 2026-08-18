using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;

namespace ServiceDesk.Api.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        string userId = GetUserId();
        _logger.LogInformation("SignalR connected: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    public async Task JoinTicket(Guid ticketId)
    {
        string userId = GetUserId();
        _logger.LogInformation("JoinTicket: {TicketId}, User: {UserId}, Connection: {ConnectionId}", ticketId, userId, Context.ConnectionId);

        bool hasAccess = await _chatService.CanAccessTicketChatAsync(ticketId, Context.ConnectionAborted);

        if (!hasAccess)
        {
            _logger.LogWarning("JoinTicket denied for User: {UserId}, Ticket: {TicketId}", userId, ticketId);
            return;
        }

        string groupName = $"ticket-{ticketId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName, Context.ConnectionAborted);
        _logger.LogInformation("User {UserId} added to group {GroupName}", userId, groupName);

        string firstName = GetClaimValue(ClaimTypes.Name);

        await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync(
            "UserJoined",
            new { userId, firstName, ticketId },
            Context.ConnectionAborted);
    }

    public async Task LeaveTicket(Guid ticketId)
    {
        string groupName = $"ticket-{ticketId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName, Context.ConnectionAborted);

        string userId = GetUserId();
        string firstName = GetClaimValue(ClaimTypes.Name);

        await Clients.Group(groupName).SendAsync(
            "UserLeft",
            new { userId, firstName, ticketId },
            Context.ConnectionAborted);
    }

    public async Task SendMessage(Guid ticketId, string content)
    {
        string userId = GetUserId();
        _logger.LogInformation("SendMessage: Ticket: {TicketId}, User: {UserId}, Content length: {Length}", ticketId, userId, content?.Length ?? 0);

        bool hasAccess = await _chatService.CanAccessTicketChatAsync(ticketId, Context.ConnectionAborted);

        if (!hasAccess)
        {
            _logger.LogWarning("SendMessage denied for User: {UserId}, Ticket: {TicketId}", userId, ticketId);
            return;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        ChatMessageDto message = await _chatService.SaveMessageAsync(ticketId, content, Context.ConnectionAborted);
        _logger.LogInformation("Message saved: {MessageId}", message.Id);

        string groupName = $"ticket-{ticketId}";
        await Clients.Group(groupName).SendAsync("ReceiveMessage", message, Context.ConnectionAborted);
        _logger.LogInformation("ReceiveMessage sent to group {GroupName}", groupName);
    }

    public async Task SendTyping(Guid ticketId)
    {
        bool hasAccess = await _chatService.CanAccessTicketChatAsync(ticketId, Context.ConnectionAborted);

        if (!hasAccess)
        {
            return;
        }

        string userId = GetUserId();
        string firstName = GetClaimValue(ClaimTypes.Name);
        string lastName = GetClaimValue("lastName");

        string groupName = $"ticket-{ticketId}";
        await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync(
            "UserTyping",
            new { userId, firstName, lastName, ticketId },
            Context.ConnectionAborted);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string userId = GetUserId();
        _logger.LogInformation("SignalR disconnected: {ConnectionId}, User: {UserId}, Error: {Error}", Context.ConnectionId, userId, exception?.Message);
        await Clients.All.SendAsync("UserDisconnected", new { userId }, Context.ConnectionAborted);
        await base.OnDisconnectedAsync(exception);
    }

    private string GetUserId() =>
        Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? string.Empty;

    private string GetClaimValue(string claimType) =>
        Context.User?.FindFirstValue(claimType) ?? string.Empty;
}
