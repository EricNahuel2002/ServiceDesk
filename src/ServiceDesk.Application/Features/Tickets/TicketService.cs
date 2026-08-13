using FluentValidation;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Common.Validation;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Catalog;
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

    public TicketService(
        ITicketRepository tickets,
        ICatalogRepository catalog,
        IUserRepository users,
        IIdentityService identity,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ICatalogVerificationService catalogVerification,
        IValidator<CreateTicketRequest> validator,
        IValidator<UpdateTicketRequest> updateValidator)
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

        foreach (TicketFileUpload file in request.Files)
        {
            ticket.Attachments.Add(new TicketAttachment
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                UploadedById = userId,
                FileName = file.FileName,
                BlobUrl = $"attachments/{ticket.Id}/{file.FileName}",
                ContentType = file.ContentType,
                SizeInBytes = file.SizeInBytes,
                Content = file.Content
            });
        }

        _tickets.Add(ticket);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<TicketDto>> GetMineAsync(CancellationToken cancellationToken) =>
        await _tickets.GetMineAsync(_currentUser.UserId, cancellationToken);

    public async Task<IReadOnlyList<TicketDto>> GetAllAsync(CancellationToken cancellationToken) =>
        await _tickets.GetAllAsync(_currentUser.CompanyId, cancellationToken);

    public async Task<IReadOnlyList<TechnicianDto>> GetTechniciansAsync(CancellationToken cancellationToken) =>
        await _users.GetTechniciansAsync(_currentUser.CompanyId, cancellationToken);

    public async Task<TicketDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        TicketDto? ticket = await _tickets.GetDtoByIdAsync(id, _currentUser.CompanyId, cancellationToken);

        return ticket ?? throw new NotFoundException($"El ticket con id {id} no existe.");
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

        await _catalogVerification.EnsurePriorityBelongsToCompanyAsync(request.PriorityId, cancellationToken);
        Status status = await _catalogVerification.EnsureStatusBelongsToCompanyAsync(request.StatusId, cancellationToken);

        ticket.AssignedToId = request.AssignedToId;
        ticket.PriorityId = request.PriorityId;
        ticket.StatusId = request.StatusId;
        ticket.ResolvedAtUtc = status.IsClosed ? DateTime.UtcNow : null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(ticket.Id, cancellationToken);
    }

    private async Task EnsureTechnicianValidAsync(Guid technicianId, CancellationToken cancellationToken)
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
}
