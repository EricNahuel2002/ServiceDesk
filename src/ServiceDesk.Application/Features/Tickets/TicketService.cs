using System.Text.Json;
using FluentValidation;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Common.Validation;
using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Application.Features.Tickets.Validators;
using ServiceDesk.Domain.Audit;
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
    private readonly IValidator<CreateTicketRequest> _validator;
    private readonly IValidator<UpdateTicketRequest> _updateValidator;
    private readonly IValidator<ResolveTicketRequest> _resolveValidator;
    private readonly IValidator<SubmitTicketFeedbackRequest> _feedbackValidator;
    private readonly IValidator<CreateTechnicianReportRequest> _technicianReportValidator;
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
        IValidator<CreateTicketRequest> validator,
        IValidator<UpdateTicketRequest> updateValidator,
        IValidator<ResolveTicketRequest> resolveValidator,
        IValidator<SubmitTicketFeedbackRequest> feedbackValidator,
        IValidator<CreateTechnicianReportRequest> technicianReportValidator,
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
        _validator = validator;
        _updateValidator = updateValidator;
        _resolveValidator = resolveValidator;
        _feedbackValidator = feedbackValidator;
        _technicianReportValidator = technicianReportValidator;
        _blobStorage = blobStorage;
        _queueStorage = queueStorage;
    }

    public async Task<TicketDto> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_validator, request, cancellationToken);

        await EnsureWithinBusinessHoursAsync(cancellationToken);

        Guid companyId = _currentUser.CompanyId;
        Guid userId = _currentUser.UserId;

        await _catalogVerification.EnsureCategoryBelongsToCompanyAsync(request.CategoryId, cancellationToken);

        Guid statusId = await FindInitialStatusIdAsync(companyId, cancellationToken);
        DateTime responseDeadline = await CalculateResponseDeadlineAsync(companyId, null, DateTime.UtcNow, cancellationToken);

        Ticket ticket = new()
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CompanyId = companyId,
            CategoryId = request.CategoryId,
            Priority = null,
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

            LogAudit(ticket, TicketAuditActions.Created, "Ticket creado", userId);

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

        await EnsureWithinBusinessHoursAsync(cancellationToken);

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

        Guid? enProgresoStatusId = await _catalog.FindStatusByNameAsync(ticket.CompanyId, "En Progreso", cancellationToken);

        if (enProgresoStatusId is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["StatusId"] = ["No se encontró el estado 'En Progreso' para tu empresa."]
            });
        }

        ticket.StartedWorkAtUtc = DateTime.UtcNow;
        ticket.StatusId = enProgresoStatusId.Value;

        LogAudit(ticket, TicketAuditActions.WorkStarted, "Trabajo iniciado", _currentUser.UserId);

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

        await EnsureWithinBusinessHoursAsync(cancellationToken);

        ApplicationUser? technician = await _users.GetByIdAsync(_currentUser.UserId, cancellationToken);

        if (technician is null)
        {
            throw new NotFoundException("El técnico actual no existe.");
        }

        TicketFinalizationPolicy.EnsureCanBeFinalizedBy(ticket, technician);

        Guid? closedStatusId = await _catalog.FindFirstClosedStatusIdAsync(_currentUser.CompanyId, cancellationToken);

        if (closedStatusId is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["StatusId"] = ["No se encontró un estado de cierre para tu empresa."]
            });
        }

        if (ticket.StatusId == closedStatusId.Value)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Ticket"] = ["El ticket ya fue finalizado."]
            });
        }

        ticket.StatusId = closedStatusId.Value;
        ticket.ResolvedAtUtc = DateTime.UtcNow;

        string? resolutionNote = null;

        if (!string.IsNullOrWhiteSpace(request.ResolutionNote))
        {
            resolutionNote = request.ResolutionNote.Trim();

            _tickets.AddComment(new TicketComment
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                AuthorId = technician.Id,
                Body = resolutionNote,
                IsInternal = false
            });
        }

        LogAudit(ticket, TicketAuditActions.Resolved, "Ticket cerrado", technician.Id, resolutionNote);

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

        Guid? previousAssignedToId = ticket.AssignedToId;

        if (request.AssignedToId.HasValue)
        {
            await EnsureTechnicianValidAsync(request.AssignedToId.Value, cancellationToken);
        }

        bool wasReassigned = request.AssignedToId.HasValue && ticket.AssignedToId != request.AssignedToId.Value;

        if (wasReassigned)
        {
            await EnsureWithinBusinessHoursAsync(cancellationToken);
        }

        bool priorityChanged = ticket.Priority != request.Priority;

        ticket.AssignedToId = request.AssignedToId ?? ticket.AssignedToId;
        ticket.Priority = request.Priority;

        if (wasReassigned)
        {
            ticket.AssignedAtUtc = DateTime.UtcNow;
        }

        if (priorityChanged && request.Priority.HasValue)
        {
            ticket.ResponseDeadlineAtUtc = await CalculateResponseDeadlineAsync(
                _currentUser.CompanyId,
                request.Priority,
                ticket.CreatedAtUtc,
                cancellationToken);
        }

        if (ticket.Priority is not null)
        {
            int slaLimitHours = await GetSlaLimitHoursAsync(
                _currentUser.CompanyId,
                ticket.Priority.Value,
                cancellationToken);

            await RefreshCurrentSlaRecordAsync(ticket, slaLimitHours, ticket.AssignedToId, cancellationToken);
        }
        else
        {
            await DeactivateCurrentSlaRecordAsync(ticket, cancellationToken);
        }

        if (wasReassigned)
        {
            string technicianName = await GetTechnicianNameAsync(request.AssignedToId!.Value, cancellationToken);
            string action = previousAssignedToId.HasValue
                ? TicketAuditActions.Reassigned
                : TicketAuditActions.Assigned;
            string description = action == TicketAuditActions.Reassigned
                ? $"Reasignado a {technicianName}"
                : $"Asignado a {technicianName}";

            LogAudit(ticket, action, description, _currentUser.UserId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (wasReassigned)
        {
            await EnqueueAssignedNotificationAsync(ticket, cancellationToken);
            await EnqueueClientAssignedNotificationAsync(ticket, cancellationToken);
        }

        return await GetByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task<TicketDto> AssignAsync(
        Guid id,
        AssignTicketRequest request,
        CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(
            new AssignTicketRequestValidator(), request, cancellationToken);

        Ticket? ticket = await _tickets.GetByIdAsync(id, _currentUser.CompanyId, cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"El ticket con id {id} no existe.");
        }

        await EnsureTechnicianValidAsync(request.AssignedToId, cancellationToken);

        await EnsureWithinBusinessHoursAsync(cancellationToken);

        bool wasReassigned = ticket.AssignedToId != request.AssignedToId;

        Guid? enEsperaStatusId = await _catalog.FindStatusByNameAsync(ticket.CompanyId, "En Espera", cancellationToken);

        if (enEsperaStatusId is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["StatusId"] = ["No se encontró el estado 'En Espera' para tu empresa."]
            });
        }

        ticket.AssignedToId = request.AssignedToId;
        ticket.StatusId = enEsperaStatusId.Value;
        ticket.AssignedAtUtc = DateTime.UtcNow;

        if (ticket.Priority is not null)
        {
            int slaLimitHours = await GetSlaLimitHoursAsync(
                ticket.CompanyId,
                ticket.Priority.Value,
                cancellationToken);

            await RefreshCurrentSlaRecordAsync(ticket, slaLimitHours, request.AssignedToId, cancellationToken);
        }

        if (wasReassigned)
        {
            string technicianName = await GetTechnicianNameAsync(request.AssignedToId, cancellationToken);
            string action = ticket.AssignedToId != request.AssignedToId && ticket.AssignedToId != null
                ? TicketAuditActions.Reassigned
                : TicketAuditActions.Assigned;
            string description = action == TicketAuditActions.Reassigned
                ? $"Reasignado a {technicianName}"
                : $"Asignado a {technicianName}";

            LogAudit(ticket, action, description, _currentUser.UserId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (wasReassigned)
        {
            await EnqueueAssignedNotificationAsync(ticket, cancellationToken);
            await EnqueueClientAssignedNotificationAsync(ticket, cancellationToken);
        }

        return await GetByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task<TicketDto> SubmitFeedbackAsync(
        Guid id,
        SubmitTicketFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_feedbackValidator, request, cancellationToken);

        Ticket? ticket = await _tickets.GetClientTicketByIdAsync(
            id,
            _currentUser.CompanyId,
            _currentUser.UserId,
            cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"El ticket con id {id} no existe.");
        }

        if (ticket.ResolvedAtUtc is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Ticket"] = ["El ticket no está finalizado."]
            });
        }

        if (ticket.Feedbacks.Any(feedback => feedback.CreatedAtUtc >= ticket.ResolvedAtUtc))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Ticket"] = ["Ya respondiste a esta encuesta de satisfacción."]
            });
        }

        TicketFeedback feedback = new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ClientId = _currentUser.UserId,
            WasSolved = request.WasSolved,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            TechnicianId = ticket.AssignedToId
        };

        if (!request.WasSolved)
        {
            await ReopenTicketAsync(ticket, cancellationToken);

            LogAudit(
                ticket,
                TicketAuditActions.Reopened,
                "Reabierto por el cliente",
                _currentUser.UserId,
                string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim());
        }
        else
        {
            string? feedbackComment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

            LogAudit(
                ticket,
                TicketAuditActions.FeedbackSubmitted,
                "Encuesta enviada",
                _currentUser.UserId,
                feedbackComment);
        }

        _tickets.AddFeedback(feedback);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task<TicketDto> CreateTechnicianReportAsync(
        Guid id,
        CreateTechnicianReportRequest request,
        CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_technicianReportValidator, request, cancellationToken);

        Ticket? ticket = await _tickets.GetClientTicketByIdAsync(
            id,
            _currentUser.CompanyId,
            _currentUser.UserId,
            cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"El ticket con id {id} no existe.");
        }

        TicketFeedback? latestFeedback = ticket.Feedbacks
            .OrderByDescending(feedback => feedback.CreatedAtUtc)
            .FirstOrDefault();

        if (latestFeedback is null
            || latestFeedback.WasSolved
            || latestFeedback.TechnicianId is null
            || ticket.TechnicianReports.Any(report => report.CreatedAtUtc >= latestFeedback.CreatedAtUtc))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Ticket"] = ["No hay una reapertura pendiente de reporte o ya enviaste un reporte por esta reapertura."]
            });
        }

        TechnicianReport report = new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ReportedById = _currentUser.UserId,
            TechnicianId = latestFeedback.TechnicianId.Value,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim()
        };

        List<string> uploadedBlobNames = new(request.Files.Count);

        try
        {
            foreach (TicketFileUpload file in request.Files)
            {
                Guid attachmentId = Guid.NewGuid();
                string blobName = BuildReportBlobName(_currentUser.CompanyId, ticket.Id, report.Id, attachmentId);

                await _blobStorage.UploadAsync(
                    blobName,
                    file.Content,
                    file.ContentType,
                    cancellationToken);

                uploadedBlobNames.Add(blobName);

                report.Attachments.Add(new TechnicianReportAttachment
                {
                    Id = attachmentId,
                    TechnicianReportId = report.Id,
                    FileName = file.FileName,
                    BlobName = blobName,
                    ContentType = file.ContentType,
                    SizeInBytes = file.SizeInBytes
                });
            }

            _tickets.AddTechnicianReport(report);

            LogAudit(
                ticket,
                TicketAuditActions.TechnicianReport,
                "Reporte técnico enviado",
                _currentUser.UserId,
                string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim());

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

    private async Task ReopenTicketAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        TicketSlaRecord? currentSlaRecord = await _tickets.GetCurrentSlaRecordAsync(ticket.Id, cancellationToken);

        if (currentSlaRecord is not null)
        {
            SlaPolicy.MarkCanceled(currentSlaRecord, DateTime.UtcNow);
            currentSlaRecord.CanceledReason = SlaRecordCancelReason.ReopenedByClientFeedback;
        }

        Guid? nuevoStatusId = await _catalog.FindStatusByNameAsync(ticket.CompanyId, "Nuevo", cancellationToken);

        if (nuevoStatusId is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["StatusId"] = ["No se encontró el estado 'Nuevo' para tu empresa."]
            });
        }

        ticket.AssignedToId = null;
        ticket.StatusId = nuevoStatusId.Value;
        ticket.ResolvedAtUtc = null;
        ticket.StartedWorkAtUtc = null;

        _tickets.AddComment(new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorId = null,
            Body = "El cliente indicó que el problema no fue resuelto. El técnico fue desasignado " +
                   "y el ticket queda pendiente de nueva asignación.",
            IsInternal = true
        });
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

    private async Task<string> GetTechnicianNameAsync(Guid technicianId, CancellationToken cancellationToken)
    {
        ApplicationUser? technician = await _users.GetByIdAsync(technicianId, cancellationToken);

        if (technician is null)
        {
            return string.Empty;
        }

        return $"{technician.FirstName} {technician.LastName}".Trim();
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

    private async Task EnsureWithinBusinessHoursAsync(CancellationToken cancellationToken)
    {
        CompanyBusinessHours? businessHours = await _slaRepository.GetBusinessHoursAsync(
            _currentUser.CompanyId,
            cancellationToken);

        if (businessHours is null || !businessHours.UseBusinessHours)
        {
            return;
        }

        if (!Domain.Sla.BusinessHoursCalculator.IsWithinBusinessHours(DateTime.UtcNow, businessHours))
        {
            throw new DomainRuleViolationException(
                "Esta acción no se puede realizar fuera del horario laboral de la empresa.");
        }
    }

    private async Task<DateTime> CalculateResponseDeadlineAsync(
        Guid companyId,
        TicketPriority? priority,
        DateTime createdAtUtc,
        CancellationToken cancellationToken)
    {
        if (priority is null)
        {
            return createdAtUtc;
        }

        SlaConfiguration? slaConfig = await _slaRepository.FindByCompanyAndPriorityAsync(
            companyId,
            priority.Value,
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

        return Domain.Sla.BusinessHoursCalculator.AddBusinessHours(
            createdAtUtc,
            slaConfig.ResponseTimeHours,
            businessHours);
    }

    private async Task<int> GetSlaLimitHoursAsync(
        Guid companyId,
        TicketPriority priority,
        CancellationToken cancellationToken)
    {
        SlaConfiguration? configuration = await _slaRepository.FindByCompanyAndPriorityAsync(
            companyId,
            priority,
            cancellationToken);

        return configuration?.ResponseTimeHours ?? 4;
    }

    private async Task RefreshCurrentSlaRecordAsync(
        Ticket ticket,
        int slaLimitHours,
        Guid? technicianId,
        CancellationToken cancellationToken)
    {
        TicketSlaRecord? current = await _tickets.GetCurrentSlaRecordAsync(ticket.Id, cancellationToken);

        if (current is not null && current.TechnicianId == technicianId)
        {
            current.ResponseDeadlineAtUtc = ticket.ResponseDeadlineAtUtc;
            current.SlaLimitHours = slaLimitHours;
            return;
        }

        if (current is not null)
        {
            current.IsCurrent = false;
        }

        _tickets.AddSlaRecord(new TicketSlaRecord
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            TechnicianId = technicianId,
            Priority = ticket.Priority!.Value,
            SlaLimitHours = slaLimitHours,
            ResponseDeadlineAtUtc = ticket.ResponseDeadlineAtUtc,
            IsCurrent = true
        });
    }

    private async Task DeactivateCurrentSlaRecordAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        TicketSlaRecord? current = await _tickets.GetCurrentSlaRecordAsync(ticket.Id, cancellationToken);

        if (current is not null)
        {
            current.IsCurrent = false;
        }
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

    private static string BuildBlobName(Guid companyId, Guid ticketId, Guid attachmentId) =>
        $"{companyId:N}/{ticketId:N}/{attachmentId:N}";

    private static string BuildReportBlobName(Guid companyId, Guid ticketId, Guid reportId, Guid attachmentId) =>
        $"{companyId:N}/{ticketId:N}/technician-reports/{reportId:N}/{attachmentId:N}";

    private void LogAudit(Ticket ticket, string action, string description, Guid? actorId, string? details = null)
    {
        _tickets.AddAuditLog(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            CompanyId = ticket.CompanyId,
            EntityType = "Ticket",
            EntityId = ticket.Id,
            Action = action,
            Description = description,
            Details = details
        });
    }

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
        if (ticket.Priority is null)
        {
            return ticket with
            {
                SlaLimitHours = 0,
                DelayMinutes = 0,
                EffectiveSlaLimitHours = 0,
                SlaPercentageElapsed = 0,
                IsOverdue = false
            };
        }

        int slaLimit = slaLimits.GetValueOrDefault(ticket.Priority.Value, 4);

        int delayMinutes = 0;
        decimal effectiveSlaLimitHours = slaLimit;

        if (ticket.AssignedAtUtc is not null)
        {
            int maxAssignmentToStartMinutes = businessHours?.MaxAssignmentToStartMinutes ?? 0;
            delayMinutes = BusinessHoursCalculator.CalculateDelayMinutes(
                ticket.AssignedAtUtc.Value,
                ticket.StartedWorkAtUtc,
                maxAssignmentToStartMinutes);
            effectiveSlaLimitHours = Math.Max(0, slaLimit - delayMinutes / 60m);
        }

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
            DelayMinutes = delayMinutes,
            EffectiveSlaLimitHours = effectiveSlaLimitHours,
            SlaPercentageElapsed = Math.Round(percentageElapsed, 1),
            IsOverdue = isOverdue
        };
    }
}
