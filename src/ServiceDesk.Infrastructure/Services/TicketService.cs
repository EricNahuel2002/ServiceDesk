using System.Linq.Expressions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Common.Validation;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Tickets;
using ServiceDesk.Infrastructure.Persistence;
using ValidationException = ServiceDesk.Application.Common.Exceptions.ValidationException;

namespace ServiceDesk.Infrastructure.Services;

public sealed class TicketService : ITicketService
{
    private static readonly Expression<Func<Ticket, TicketDto>> TicketProjection = ticket => new TicketDto
    {
        Id = ticket.Id,
        Title = ticket.Title,
        Description = ticket.Description,
        CompanyId = ticket.CompanyId,
        CategoryId = ticket.CategoryId,
        CategoryName = ticket.Category!.Name,
        PriorityId = ticket.PriorityId,
        PriorityName = ticket.Priority!.Name,
        StatusId = ticket.StatusId,
        StatusName = ticket.Status!.Name,
        CreatedById = ticket.CreatedById,
        AssignedToId = ticket.AssignedToId,
        CreatedAtUtc = ticket.CreatedAtUtc,
        UpdatedAtUtc = ticket.UpdatedAtUtc,
        Attachments = ticket.Attachments
            .Select(attachment => new TicketAttachmentDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                SizeInBytes = attachment.SizeInBytes,
                BlobUrl = attachment.BlobUrl
            })
            .ToList()
    };

    private readonly ServiceDeskDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IValidator<CreateTicketRequest> _validator;
    private readonly IValidator<UpdateTicketRequest> _updateValidator;

    public TicketService(
        ServiceDeskDbContext context,
        ICurrentUserService currentUser,
        UserManager<ApplicationUser> userManager,
        IValidator<CreateTicketRequest> validator,
        IValidator<UpdateTicketRequest> updateValidator)
    {
        _context = context;
        _currentUser = currentUser;
        _userManager = userManager;
        _validator = validator;
        _updateValidator = updateValidator;
    }

    public async Task<TicketDto> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_validator, request, cancellationToken);

        Guid companyId = _currentUser.CompanyId;
        Guid userId = _currentUser.UserId;

        await EnsureCatalogValidAsync(
            _context.Categories,
            "CategoryId",
            category => category.Id == request.CategoryId && category.CompanyId == companyId && category.IsActive,
            cancellationToken);

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

        _context.Tickets.Add(ticket);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<TicketDto>> GetMineAsync(CancellationToken cancellationToken)
    {
        List<TicketDto> tickets = await _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.CreatedById == _currentUser.UserId)
            .OrderByDescending(ticket => ticket.CreatedAtUtc)
            .Select(TicketProjection)
            .ToListAsync(cancellationToken);

        return tickets;
    }

    public async Task<IReadOnlyList<TicketDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<TicketDto> tickets = await _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.CompanyId == _currentUser.CompanyId)
            .OrderByDescending(ticket => ticket.CreatedAtUtc)
            .Select(TicketProjection)
            .ToListAsync(cancellationToken);

        return tickets;
    }

    public async Task<IReadOnlyList<TechnicianDto>> GetTechniciansAsync(CancellationToken cancellationToken)
    {
        List<TechnicianDto> technicians = await _context.Users
            .AsNoTracking()
            .Where(user => user.IsActive
                && user.CompanyId == _currentUser.CompanyId
                && _context.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == _context.Roles
                    .Where(role => role.Name == Roles.Tecnico)
                    .Select(role => role.Id)
                    .FirstOrDefault()))
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(user => new TechnicianDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        return technicians;
    }

    public async Task<TicketDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        TicketDto? ticket = await _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == id && ticket.CompanyId == _currentUser.CompanyId)
            .Select(TicketProjection)
            .SingleOrDefaultAsync(cancellationToken);

        return ticket ?? throw new NotFoundException($"El ticket con id {id} no existe.");
    }

    public async Task<TicketDto> UpdateAsync(
        Guid id,
        UpdateTicketRequest request,
        CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_updateValidator, request, cancellationToken);

        Ticket? ticket = await _context.Tickets
            .Where(ticket => ticket.Id == id && ticket.CompanyId == _currentUser.CompanyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"El ticket con id {id} no existe.");
        }

        await EnsureTechnicianValidAsync(request.AssignedToId, cancellationToken);

        await EnsureCatalogValidAsync(
            _context.Priorities,
            "PriorityId",
            priority => priority.Id == request.PriorityId && priority.CompanyId == ticket.CompanyId && priority.IsActive,
            cancellationToken);

        Status? status = await _context.Statuses
            .AsNoTracking()
            .Where(item => item.Id == request.StatusId && item.CompanyId == ticket.CompanyId && item.IsActive)
            .SingleOrDefaultAsync(cancellationToken);

        if (status is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["StatusId"] = ["El estado indicado no existe, no pertenece a tu empresa o está inactivo."]
            });
        }

        ticket.AssignedToId = request.AssignedToId;
        ticket.PriorityId = request.PriorityId;
        ticket.StatusId = request.StatusId;
        ticket.ResolvedAtUtc = status.IsClosed ? DateTime.UtcNow : null;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(ticket.Id, cancellationToken);
    }

    private async Task EnsureTechnicianValidAsync(Guid technicianId, CancellationToken cancellationToken)
    {
        ApplicationUser? technician = await _userManager.FindByIdAsync(technicianId.ToString());

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

        if (!await _userManager.IsInRoleAsync(technician, Roles.Tecnico))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["AssignedToId"] = ["El usuario indicado no tiene el rol de técnico."]
            });
        }
    }

    private async Task<Guid> FindInitialStatusIdAsync(Guid companyId, CancellationToken cancellationToken)
    {
        Guid? statusId = await _context.Statuses
            .Where(status => status.CompanyId == companyId && status.Name == "Nuevo" && status.IsActive)
            .Select(status => (Guid?)status.Id)
            .SingleOrDefaultAsync(cancellationToken);

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
        Guid? priorityId = await _context.Priorities
            .Where(priority => priority.CompanyId == companyId && priority.Name == "Media" && priority.IsActive)
            .Select(priority => (Guid?)priority.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (priorityId is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["PriorityId"] = ["No se encontró la prioridad por defecto 'Media' para tu empresa."]
            });
        }

        return priorityId.Value;
    }

    private static async Task EnsureCatalogValidAsync<TEntity>(
        IQueryable<TEntity> entities,
        string propertyName,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken)
    {
        bool exists = await entities.AnyAsync(predicate, cancellationToken);

        if (!exists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [propertyName] = ["El catálogo indicado no existe, no pertenece a tu empresa o está inactivo."]
            });
        }
    }
}
