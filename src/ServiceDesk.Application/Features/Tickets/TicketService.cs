using FluentValidation;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Common.Validation;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Tickets;
using ValidationException = ServiceDesk.Application.Common.Exceptions.ValidationException;

namespace ServiceDesk.Application.Features.Tickets;

public sealed class TicketService : ITicketService
{
    private readonly ITicketRepository _tickets;
    private readonly ICatalogRepository _catalog;
    private readonly IUserRepository _users;
    private readonly IIdentityService _identity;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ICatalogVerificationService _catalogVerification;
    private readonly IValidator<CreateTicketRequest> _validator;
    private readonly IValidator<UpdateTicketRequest> _updateValidator;
    private readonly IValidator<ResolveTicketRequest> _resolveValidator;
    private readonly IEmailService _email;
    private readonly IBlobStorageService _blobStorage;

    public TicketService(
        ITicketRepository tickets,
        ICatalogRepository catalog,
        IUserRepository users,
        IIdentityService identity,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ICatalogVerificationService catalogVerification,
        IValidator<CreateTicketRequest> validator,
        IValidator<UpdateTicketRequest> updateValidator,
        IValidator<ResolveTicketRequest> resolveValidator,
        IEmailService email,
        IBlobStorageService blobStorage)
    {
        _tickets = tickets;
        _catalog = catalog;
        _users = users;
        _identity = identity;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _catalogVerification = catalogVerification;
        _validator = validator;
        _updateValidator = updateValidator;
        _resolveValidator = resolveValidator;
        _email = email;
        _blobStorage = blobStorage;
    }

    public async Task<TicketDto> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_validator, request, cancellationToken);

        Guid companyId = _currentUser.CompanyId;
        Guid userId = _currentUser.UserId;

        await _catalogVerification.EnsureCategoryBelongsToCompanyAsync(request.CategoryId, cancellationToken);

        Guid statusId = await FindInitialStatusIdAsync(companyId, cancellationToken);
        Guid priorityId = await FindDefaultPriorityIdAsync(companyId, cancellationToken);

        Ticket ticket = new()
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CompanyId = companyId,
            CategoryId = request.CategoryId,
            PriorityId = priorityId,
            StatusId = statusId,
            CreatedById = userId
        };

        List<TicketAttachment> attachments = new(request.Files.Count);
        List<string> uploadedBlobNames = new(request.Files.Count);

        try
        {
            foreach (TicketFileUpload file in request.Files)
            {
                Guid attachmentId = Guid.NewGuid();
                string blobName = BuildBlobName(companyId, ticket.Id, attachmentId);

                await _blobStorage.UploadAsync(
                    blobName,
                    file.Content,
                    file.ContentType,
                    cancellationToken);

                uploadedBlobNames.Add(blobName);

                attachments.Add(new TicketAttachment
                {
                    Id = attachmentId,
                    TicketId = ticket.Id,
                    UploadedById = userId,
                    FileName = file.FileName,
                    BlobName = blobName,
                    ContentType = file.ContentType,
                    SizeInBytes = file.SizeInBytes
                });
            }

            ticket.Attachments = attachments;

            _tickets.Add(ticket);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (string blobName in uploadedBlobNames)
            {
                await _blobStorage.DeleteAsync(blobName, cancellationToken);
            }

            throw;
        }

        return await GetByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<TicketDto>> GetMineAsync(CancellationToken cancellationToken) =>
        await _tickets.GetMineAsync(_currentUser.UserId, cancellationToken);

    public async Task<IReadOnlyList<TicketDto>> GetAssignedToMeAsync(CancellationToken cancellationToken) =>
        await _tickets.GetAssignedToAsync(_currentUser.UserId, cancellationToken);

    public async Task<IReadOnlyList<TicketDto>> GetAllAsync(CancellationToken cancellationToken) =>
        await _tickets.GetAllAsync(_currentUser.CompanyId, cancellationToken);

    public async Task<IReadOnlyList<TechnicianDto>> GetTechniciansAsync(CancellationToken cancellationToken) =>
        await _users.GetTechniciansAsync(_currentUser.CompanyId, cancellationToken);

    public async Task<TicketDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        TicketDto? ticket = await _tickets.GetDtoByIdAsync(id, _currentUser.CompanyId, cancellationToken);

        return ticket ?? throw new NotFoundException($"El ticket con id {id} no existe.");
    }

    public async Task<TicketDto> ResolveAsync(
        Guid id,
        ResolveTicketRequest request,
        CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_resolveValidator, request, cancellationToken);

        Ticket? ticket = await _tickets.GetAssignedTicketByIdAsync(
            id,
            _currentUser.CompanyId,
            _currentUser.UserId,
            cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"El ticket con id {id} no existe.");
        }

        ApplicationUser? technician = await _users.GetByIdAsync(_currentUser.UserId, cancellationToken);

        if (technician is null)
        {
            throw new NotFoundException("El técnico actual no existe.");
        }

        TicketFinalizationPolicy.EnsureCanBeFinalizedBy(ticket, technician);

        Status? currentStatus = await _catalog.GetStatusByIdAsync(ticket.StatusId, cancellationToken);

        if (currentStatus is not null && currentStatus.IsClosed)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Ticket"] = ["El ticket ya fue finalizado."]
            });
        }

        Guid? closedStatusId = await _catalog.FindFirstClosedStatusIdAsync(_currentUser.CompanyId, cancellationToken);

        if (closedStatusId is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["StatusId"] = ["No se encontró un estado de cierre para tu empresa."]
            });
        }

        ticket.StatusId = closedStatusId.Value;
        ticket.ResolvedAtUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.ResolutionNote))
        {
            _tickets.AddComment(new TicketComment
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                AuthorId = technician.Id,
                Body = request.ResolutionNote.Trim(),
                IsInternal = false
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task<TicketDto> UpdateAsync(
        Guid id,
        UpdateTicketRequest request,
        CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_updateValidator, request, cancellationToken);

        Ticket? ticket = await _tickets.GetByIdAsync(id, _currentUser.CompanyId, cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"El ticket con id {id} no existe.");
        }

        ApplicationUser technician = await EnsureTechnicianValidAsync(request.AssignedToId, cancellationToken);

        bool wasReassigned = ticket.AssignedToId != request.AssignedToId;

        await _catalogVerification.EnsurePriorityBelongsToCompanyAsync(request.PriorityId, cancellationToken);
        Status status = await _catalogVerification.EnsureStatusBelongsToCompanyAsync(request.StatusId, cancellationToken);

        ticket.AssignedToId = request.AssignedToId;
        ticket.PriorityId = request.PriorityId;
        ticket.StatusId = request.StatusId;
        ticket.ResolvedAtUtc = status.IsClosed ? DateTime.UtcNow : null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (wasReassigned)
        {
            await SendAssignedNotificationAsync(ticket, technician, cancellationToken);
        }

        return await GetByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task<AttachmentDownloadResult> DownloadAttachmentAsync(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        TicketAttachment? attachment = await _tickets.GetAttachmentByIdAsync(
            ticketId,
            attachmentId,
            _currentUser.CompanyId,
            cancellationToken);

        if (attachment is null)
        {
            throw new NotFoundException($"El archivo adjunto {attachmentId} no existe.");
        }

        Stream content = await _blobStorage.DownloadAsync(attachment.BlobName, cancellationToken);

        return new AttachmentDownloadResult
        {
            Content = content,
            ContentType = attachment.ContentType,
            FileName = attachment.FileName
        };
    }

    private async Task<ApplicationUser> EnsureTechnicianValidAsync(Guid technicianId, CancellationToken cancellationToken)
    {
        ApplicationUser? technician = await _users.GetByIdAsync(technicianId, cancellationToken);

        if (technician is null || !technician.IsActive)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["AssignedToId"] = ["El técnico indicado no existe o está inactivo."]
            });
        }

        if (technician.CompanyId != _currentUser.CompanyId)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["AssignedToId"] = ["El técnico indicado no pertenece a tu empresa."]
            });
        }

        if (!await _identity.IsInRoleAsync(technician, Roles.Tecnico, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["AssignedToId"] = ["El usuario indicado no tiene el rol de técnico."]
            });
        }

        return technician;
    }

    private async Task SendAssignedNotificationAsync(
        Ticket ticket,
        ApplicationUser technician,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(technician.Email))
        {
            return;
        }

        Priority? priority = await _catalog.GetPriorityByIdAsync(ticket.PriorityId, cancellationToken);

        string subject = "Se te asignó un ticket";
        string body = $"""
            Hola {technician.FirstName},

            Se te asignó el siguiente ticket:

            Título: {ticket.Title}
            Descripción: {ticket.Description}
            Prioridad: {priority?.Name ?? "No especificada"}

            Saludos,
            ServiceDesk
            """;

        await _email.SendAsync(technician.Email, subject, body, cancellationToken);
    }

    private async Task<Guid> FindInitialStatusIdAsync(Guid companyId, CancellationToken cancellationToken)
    {
        Guid? statusId = await _catalog.FindInitialStatusIdAsync(companyId, cancellationToken);

        if (statusId is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["StatusId"] = ["No se encontró el estado inicial 'Nuevo' para tu empresa."]
            });
        }

        return statusId.Value;
    }

    private async Task<Guid> FindDefaultPriorityIdAsync(Guid companyId, CancellationToken cancellationToken)
    {
        Guid? priorityId = await _catalog.FindDefaultPriorityIdAsync(companyId, cancellationToken);

        if (priorityId is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["PriorityId"] = ["No se encontró la prioridad por defecto 'Media' para tu empresa."]
            });
        }

        return priorityId.Value;
    }

    private static string BuildBlobName(Guid companyId, Guid ticketId, Guid attachmentId) =>
        $"{companyId:N}/{ticketId:N}/{attachmentId:N}";
}
