using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Tickets;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly IChatService _chatService;

    public TicketsController(ITicketService ticketService, IChatService chatService)
    {
        _ticketService = ticketService;
        _chatService = chatService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TicketDto>> Create(
        [FromForm] CreateTicketRequest request,
        IFormFileCollection? files,
        CancellationToken cancellationToken)
    {
        List<TicketFileUpload> uploads = await ReadFilesAsync(files, cancellationToken);

        CreateTicketRequest requestWithFiles = request with { Files = uploads };

        try
        {
            TicketDto ticket = await _ticketService.CreateAsync(requestWithFiles, cancellationToken);

            return CreatedAtAction(nameof(GetMine), new { }, ticket);
        }
        finally
        {
            foreach (TicketFileUpload upload in uploads)
            {
                await upload.Content.DisposeAsync();
            }
        }
    }

    [HttpGet("{ticketId:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DownloadAttachment(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        AttachmentDownloadResult result = await _ticketService.DownloadAttachmentAsync(
            ticketId,
            attachmentId,
            cancellationToken);

        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetMine(CancellationToken cancellationToken)
    {
        IReadOnlyList<TicketDto> tickets = await _ticketService.GetMineAsync(cancellationToken);

        return Ok(tickets);
    }

    [HttpGet("{ticketId:guid}/chat")]
    [ProducesResponseType(typeof(IReadOnlyList<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<ChatMessageDto>>> GetChatHistory(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatMessageDto> messages = await _chatService.GetHistoryAsync(ticketId, cancellationToken);

        return Ok(messages);
    }

    private static async Task<List<TicketFileUpload>> ReadFilesAsync(
        IFormFileCollection? files,
        CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
        {
            return [];
        }

        List<TicketFileUpload> uploads = new(files.Count);

        foreach (IFormFile file in files)
        {
            MemoryStream stream = new();
            await file.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;

            uploads.Add(new TicketFileUpload
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                SizeInBytes = file.Length,
                Content = stream
            });
        }

        return uploads;
    }
}
