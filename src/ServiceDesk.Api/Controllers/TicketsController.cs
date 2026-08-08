using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Tickets;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
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
        CreateTicketRequest requestWithFiles = request with
        {
            Files = await ReadFilesAsync(files, cancellationToken)
        };

        TicketDto ticket = await _ticketService.CreateAsync(requestWithFiles, cancellationToken);

        return CreatedAtAction(nameof(GetMine), new { }, ticket);
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
            await using MemoryStream stream = new();
            await file.CopyToAsync(stream, cancellationToken);

            uploads.Add(new TicketFileUpload
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                SizeInBytes = file.Length,
                Content = stream.ToArray()
            });
        }

        return uploads;
    }
}
