using System.Text.Json;
using FluentValidation;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Common.Validation;
using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Application.DTOs.Catalog;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Sla;
using ServiceDesk.Domain.Tickets;
using ValidationException = ServiceDesk.Application.Common.Exceptions.ValidationException;

namespace ServiceDesk.Application.Features.Tickets;

public sealed class TicketService : ITicketService
{
    private readonly ITicketRepository _tickets;
    private readonly ICatalogRepository _catalog;
    private readonly ISlaRepository _slaRepository;
    private readonly IUserRepository _users;
    private readonly IIdentityService _identity;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ICatalogVerificationService _catalogVerification;
    private readonly IBusinessHoursCalculator _businessHoursCalculator;
    private readonly IValidator<CreateTicketRequest> _validator;
    private readonly IValidator<UpdateTicketRequest> _updateValidator;
    private readonly IValidator<ResolveTicketRequest> _resolveValidator;
    private readonly IBlobStorageService _blobStorage;
    private readonly IQueueStorageService _queueStorage;

    public TicketService(
        ITicketRepository tickets,
        ICatalogRepository catalog,
        ISlaRepository slaRepository,
        IUserRepository users,
        IIdentityService identity,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ICatalogVerificationService catalogVerification,
        IBusinessHoursCalculator businessHoursCalculator,
        IValidator<CreateTicketRequest> validator,
        IValidator<UpdateTicketRequest> updateValidator,
        IValidator<ResolveTicketRequest> resolveValidator,
        IBlobStorageService blobStorage,
        IQueueStorageService queueStorage)
    {
        _tickets = tickets;
        _catalog = catalog;
        _slaRepository = slaRepository;
        _users = users;
        _identity = identity;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _catalogVerification = catalogVerification;
        _businessHoursCalculator = businessHoursCalculator;
        _validator = validator;
        _updateValidator = updateValidator;
        _resolveValidator = resolveValidator;
        _blobStorage = blobStorage;
        _queueStorage = queueStorage;
    }

    public async Task<TicketDto> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_validator, request, cancellationToken);

        Guid companyId = _currentUser.CompanyId;
        Guid userId = _currentUser.UserId;

        await _catalogVerification.EnsureCategoryBelongsToCompanyAsync(request.CategoryId, cancellationToken);

        Guid statusId = await FindInitialStatusIdAsync(companyId, cancellationToken);
        TicketPriority priority = TicketPriority.Media;
        DateTime responseDeadline = await CalculateResponseDeadlineAsync(companyId, priority, DateTime.UtcNow, cancellationToken);

        Ticket ticket = new()
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CompanyId = companyId,
            CategoryId = request.CategoryId,
            Priority = priority,
            StatusId = statusId,
            CreatedById = userId,
            ResponseDeadlineAtUtc = responseDeadline
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

    public async Task<IReadOnlyList<TicketDto>> GetMineAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<TicketDto> tickets = await _tickets.GetMineAsync(_currentUser.UserId, cancellationToken);
        return await EnrichWithSlaDataAsync(tickets, cancellationToken);
    }

    public async Task<IReadOnlyList<TicketDto>> GetAssignedToMeAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<TicketDto> tickets = await _tickets.GetAssignedToAsync(_currentUser.UserId, cancellationToken);
        return await EnrichWithSlaDataAsync(tickets, cancellationToken);
    }

    public async Task<IReadOnlyList<TicketDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<TicketDto> tickets = await _tickets.GetAllAsync(_currentUser.CompanyId, cancellationToken);
        return await EnrichWithSlaDataAsync(tickets, cancellationToken);
    }

    public async Task<IReadOnlyList<TechnicianDto>> GetTechniciansAsync(CancellationToken cancellationToken) =>
        await _users.GetTechniciansAsync(_currentUser.CompanyId, cancellationToken);

    public async Task<TicketDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        TicketDto? ticket = await _tickets.GetDtoByIdAsync(id, _currentUser.CompanyId, cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"El ticket con id {id} no existe.");
        }

        return await EnrichSingleSlaAsync(ticket, cancellationToken);
    }

    public async Task<TicketDto> StartWorkAsync(Guid id, CancellationToken cancellationToken)
    {
        Ticket? ticket = await _tickets.GetAssignedTicketByIdAsync(
            id,
            _currentUser.CompanyId,
            _currentUser.UserId,
            cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"El ticket con id {id} no existe o no está asignado a ti.");
        }

        ApplicationUser? technician = await _users.GetByIdAsync(_currentUser.UserId, cancellationToken);

        if (technician is null || !technician.IsActive)
        {
            throw new NotFoundException("El técnico actual no existe o está inactivo.");
        }

        if (!technician.IsInRole(Roles.Tecnico))
        {
            throw new DomainRuleViolationException("Solo un usuario con el rol de técnico puede iniciar trabajo en un ticket.");
        }

        if (ticket.StartedWorkAtUtc is not null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Ticket"] = ["El trabajo en este ticket ya fue iniciado."]
            });
        }

        Guid? enProgresoStatusId = await FindStatusByNameAsync(ticket.CompanyId, "En Progreso", cancellationToken);

        if (enProgresoStatusId is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["StatusId"] = ["No se encontró el estado 'En Progreso' para tu empresa."]
            });
        }

        ticket.StartedWorkAtUtc = DateTime.UtcNow;
        ticket.StatusId = enProgresoStatusId.Value;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await EnqueueClientWorkNotificationAsync(ticket.Id, NotificationEvents.WorkStarted, cancellationToken);

        return await GetByIdAsync(ticket.Id, cancellationToken);
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

        await EnqueueClientWorkNotificationAsync(ticket.Id, NotificationEvents.WorkFinished, cancellationToken);

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

        await EnsureTechnicianValidAsync(request.AssignedToId, cancellationToken);

        bool wasReassigned = ticket.AssignedToId != request.AssignedToId;

        Status status = await _catalogVerification.EnsureStatusBelongsToCompanyAsync(request.StatusId, cancellationToken);

        bool priorityChanged = ticket.Priority != request.Priority;

        bool wasResolved = ticket.ResolvedAtUtc is null && status.IsClosed;

        ticket.AssignedToId = request.AssignedToId;
        ticket.Priority = request.Priority;
        ticket.StatusId = request.StatusId;
        ticket.ResolvedAtUtc = status.IsClosed ? DateTime.UtcNow : null;

        if (priorityChanged)
        {
            ticket.ResponseDeadlineAtUtc = await CalculateResponseDeadlineAsync(
                _currentUser.CompanyId,
                request.Priority,
                ticket.CreatedAtUtc,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (wasReassigned)
        {
            await EnqueueAssignedNotificationAsync(ticket, cancellationToken);
            await EnqueueClientAssignedNotificationAsync(ticket, cancellationToken);
        }

        if (wasResolved)
        {
            await EnqueueClientWorkNotificationAsync(ticket.Id, NotificationEvents.WorkFinished, cancellationToken);
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

    private async Task<DateTime> CalculateResponseDeadlineAsync(
        Guid companyId,
        TicketPriority priority,
        DateTime createdAtUtc,
        CancellationToken cancellationToken)
    {
        SlaConfiguration? slaConfig = await _slaRepository.FindByCompanyAndPriorityAsync(
            companyId,
            priority,
            cancellationToken);

        if (slaConfig is null)
        {
            return createdAtUtc.AddHours(4);
        }

        CompanyBusinessHours? businessHours = await _slaRepository.GetBusinessHoursAsync(
            companyId,
            cancellationToken);

        if (businessHours is null || !businessHours.UseBusinessHours)
        {
            return createdAtUtc.AddHours(slaConfig.ResponseTimeHours);
        }

        return _businessHoursCalculator.AddBusinessHours(
            createdAtUtc,
            slaConfig.ResponseTimeHours,
            businessHours);
    }

    private async Task EnqueueAssignedNotificationAsync(
        Ticket ticket,
        CancellationToken cancellationToken)
    {
        TicketAssignedNotification notification = new()
        {
            EventType = NotificationEvents.TicketAssigned,
            TicketId = ticket.Id
        };

        string payload = JsonSerializer.Serialize(notification);

        await _queueStorage.EnqueueAsync(payload, cancellationToken);
    }

    private async Task EnqueueClientAssignedNotificationAsync(
        Ticket ticket,
        CancellationToken cancellationToken)
    {
        TicketAssignedNotification notification = new()
        {
            EventType = NotificationEvents.TicketAssignedToClient,
            TicketId = ticket.Id
        };

        string payload = JsonSerializer.Serialize(notification);

        await _queueStorage.EnqueueClientNotificationAsync(payload, cancellationToken);
    }

    private async Task EnqueueClientWorkNotificationAsync(
        Guid ticketId,
        string eventType,
        CancellationToken cancellationToken)
    {
        TicketAssignedNotification notification = new()
        {
            EventType = eventType,
            TicketId = ticketId
        };

        string payload = JsonSerializer.Serialize(notification);

        await _queueStorage.EnqueueClientWorkNotificationAsync(payload, cancellationToken);
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

    private async Task<Guid?> FindStatusByNameAsync(Guid companyId, string name, CancellationToken cancellationToken)
    {
        IReadOnlyList<StatusDto> statuses = await _catalog.GetAllStatusesAsync(companyId, cancellationToken);
        StatusDto? status = statuses.FirstOrDefault(s => s.Name == name);
        return status?.Id;
    }

    private static string BuildBlobName(Guid companyId, Guid ticketId, Guid attachmentId) =>
        $"{companyId:N}/{ticketId:N}/{attachmentId:N}";

    private async Task<IReadOnlyList<TicketDto>> EnrichWithSlaDataAsync(
        IReadOnlyList<TicketDto> tickets,
        CancellationToken cancellationToken)
    {
        if (tickets.Count == 0)
        {
            return tickets;
        }

        Guid companyId = _currentUser.CompanyId;

        IReadOnlyList<SlaConfiguration> slaConfigs = await _slaRepository.GetByCompanyAsync(
            companyId,
            cancellationToken);

        Dictionary<TicketPriority, int> slaLimits = slaConfigs.ToDictionary(
            c => c.Priority,
            c => c.ResponseTimeHours);

        CompanyBusinessHours? businessHours = await _slaRepository.GetBusinessHoursAsync(
            companyId,
            cancellationToken);

        bool useBusinessHours = businessHours is not null && businessHours.UseBusinessHours;

        List<TicketDto> enriched = new(tickets.Count);

        foreach (TicketDto ticket in tickets)
        {
            enriched.Add(ComputeSlaFields(ticket, slaLimits, businessHours, useBusinessHours));
        }

        return enriched;
    }

    private async Task<TicketDto> EnrichSingleSlaAsync(
        TicketDto ticket,
        CancellationToken cancellationToken)
    {
        Guid companyId = _currentUser.CompanyId;

        IReadOnlyList<SlaConfiguration> slaConfigs = await _slaRepository.GetByCompanyAsync(
            companyId,
            cancellationToken);

        Dictionary<TicketPriority, int> slaLimits = slaConfigs.ToDictionary(
            c => c.Priority,
            c => c.ResponseTimeHours);

        CompanyBusinessHours? businessHours = await _slaRepository.GetBusinessHoursAsync(
            companyId,
            cancellationToken);

        bool useBusinessHours = businessHours is not null && businessHours.UseBusinessHours;

        return ComputeSlaFields(ticket, slaLimits, businessHours, useBusinessHours);
    }

    private static TicketDto ComputeSlaFields(
        TicketDto ticket,
        Dictionary<TicketPriority, int> slaLimits,
        CompanyBusinessHours? businessHours,
        bool useBusinessHours)
    {
        int slaLimit = slaLimits.GetValueOrDefault(ticket.Priority, 4);

        decimal percentageElapsed = 0;
        bool isOverdue = false;

        if (ticket.StartedWorkAtUtc is not null)
        {
            DateTime now = DateTime.UtcNow;

            if (useBusinessHours && businessHours is not null)
            {
                percentageElapsed = BusinessHoursCalculator.CalculatePercentageElapsed(
                    ticket.StartedWorkAtUtc.Value,
                    now,
                    businessHours,
                    slaLimit);
            }
            else
            {
                TimeSpan elapsed = now - ticket.StartedWorkAtUtc.Value;
                percentageElapsed = slaLimit > 0
                    ? (decimal)(elapsed.TotalHours / slaLimit * 100)
                    : 0;
            }

            isOverdue = ticket.ResolvedAtUtc is not null
                ? ticket.ResolvedAtUtc.Value > ticket.ResponseDeadlineAtUtc
                : percentageElapsed >= 100;
        }

        return ticket with
        {
            SlaLimitHours = slaLimit,
            SlaPercentageElapsed = Math.Round(percentageElapsed, 1),
            IsOverdue = isOverdue
        };
    }
}
