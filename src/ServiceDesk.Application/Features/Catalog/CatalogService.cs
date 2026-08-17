using FluentValidation;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Common.Validation;
using ServiceDesk.Application.DTOs.Catalog;
using ServiceDesk.Domain.Catalog;
using ValidationException = ServiceDesk.Application.Common.Exceptions.ValidationException;

namespace ServiceDesk.Application.Features.Catalog;

public sealed class CatalogService : ICatalogService
{
    private readonly ICatalogRepository _catalog;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCategoryRequest> _createCategoryValidator;
    private readonly IValidator<UpdateCategoryRequest> _updateCategoryValidator;
    private readonly IValidator<CreatePriorityRequest> _createPriorityValidator;
    private readonly IValidator<UpdatePriorityRequest> _updatePriorityValidator;
    private readonly IValidator<CreateStatusRequest> _createStatusValidator;
    private readonly IValidator<UpdateStatusRequest> _updateStatusValidator;

    public CatalogService(
        ICatalogRepository catalog,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IValidator<CreateCategoryRequest> createCategoryValidator,
        IValidator<UpdateCategoryRequest> updateCategoryValidator,
        IValidator<CreatePriorityRequest> createPriorityValidator,
        IValidator<UpdatePriorityRequest> updatePriorityValidator,
        IValidator<CreateStatusRequest> createStatusValidator,
        IValidator<UpdateStatusRequest> updateStatusValidator)
    {
        _catalog = catalog;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _createCategoryValidator = createCategoryValidator;
        _updateCategoryValidator = updateCategoryValidator;
        _createPriorityValidator = createPriorityValidator;
        _updatePriorityValidator = updatePriorityValidator;
        _createStatusValidator = createStatusValidator;
        _updateStatusValidator = updateStatusValidator;
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        _catalog.GetActiveCategoriesAsync(_currentUser.CompanyId, cancellationToken);

    public Task<IReadOnlyList<PriorityDto>> GetPrioritiesAsync(CancellationToken cancellationToken) =>
        _catalog.GetActivePrioritiesAsync(_currentUser.CompanyId, cancellationToken);

    public Task<IReadOnlyList<StatusDto>> GetStatusesAsync(CancellationToken cancellationToken) =>
        _catalog.GetActiveStatusesAsync(_currentUser.CompanyId, cancellationToken);

    public Task<IReadOnlyList<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken) =>
        _catalog.GetAllCategoriesAsync(_currentUser.CompanyId, cancellationToken);

    public Task<IReadOnlyList<PriorityDto>> GetAllPrioritiesAsync(CancellationToken cancellationToken) =>
        _catalog.GetAllPrioritiesAsync(_currentUser.CompanyId, cancellationToken);

    public Task<IReadOnlyList<StatusDto>> GetAllStatusesAsync(CancellationToken cancellationToken) =>
        _catalog.GetAllStatusesAsync(_currentUser.CompanyId, cancellationToken);

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_createCategoryValidator, request, cancellationToken);

        if (await _catalog.CategoryNameExistsAsync(_currentUser.CompanyId, request.Name, cancellationToken: cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Name", ["Ya existe una categoría con ese nombre."] }
            });
        }

        Category category = new()
        {
            CompanyId = _currentUser.CompanyId,
            Name = request.Name
        };

        await _catalog.AddCategoryAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CategoryDto { Id = category.Id, Name = category.Name, IsActive = category.IsActive };
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_updateCategoryValidator, request, cancellationToken);

        Category? category = await _catalog.GetCategoryByIdAsync(id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException($"La categoría con id {id} no existe.");
        }

        if (await _catalog.CategoryNameExistsAsync(_currentUser.CompanyId, request.Name, id, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Name", ["Ya existe una categoría con ese nombre."] }
            });
        }

        category.Name = request.Name;
        category.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CategoryDto { Id = category.Id, Name = category.Name, IsActive = category.IsActive };
    }

    public async Task<PriorityDto> CreatePriorityAsync(CreatePriorityRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_createPriorityValidator, request, cancellationToken);

        if (await _catalog.PriorityNameExistsAsync(_currentUser.CompanyId, request.Name, cancellationToken: cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Name", ["Ya existe una prioridad con ese nombre."] }
            });
        }

        Priority priority = new()
        {
            CompanyId = _currentUser.CompanyId,
            Name = request.Name,
            SortOrder = request.SortOrder
        };

        await _catalog.AddPriorityAsync(priority, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PriorityDto { Id = priority.Id, Name = priority.Name, SortOrder = priority.SortOrder, IsActive = priority.IsActive };
    }

    public async Task<PriorityDto> UpdatePriorityAsync(Guid id, UpdatePriorityRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_updatePriorityValidator, request, cancellationToken);

        Priority? priority = await _catalog.GetPriorityByIdAsync(id, cancellationToken);

        if (priority is null)
        {
            throw new NotFoundException($"La prioridad con id {id} no existe.");
        }

        if (await _catalog.PriorityNameExistsAsync(_currentUser.CompanyId, request.Name, id, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Name", ["Ya existe una prioridad con ese nombre."] }
            });
        }

        priority.Name = request.Name;
        priority.SortOrder = request.SortOrder;
        priority.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PriorityDto { Id = priority.Id, Name = priority.Name, SortOrder = priority.SortOrder, IsActive = priority.IsActive };
    }

    public async Task<StatusDto> CreateStatusAsync(CreateStatusRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_createStatusValidator, request, cancellationToken);

        if (await _catalog.StatusNameExistsAsync(_currentUser.CompanyId, request.Name, cancellationToken: cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Name", ["Ya existe un estado con ese nombre."] }
            });
        }

        Status status = new()
        {
            CompanyId = _currentUser.CompanyId,
            Name = request.Name,
            SortOrder = request.SortOrder,
            IsClosed = request.IsClosed
        };

        await _catalog.AddStatusAsync(status, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StatusDto { Id = status.Id, Name = status.Name, SortOrder = status.SortOrder, IsClosed = status.IsClosed, IsActive = status.IsActive };
    }

    public async Task<StatusDto> UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_updateStatusValidator, request, cancellationToken);

        Status? status = await _catalog.GetStatusByIdAsync(id, cancellationToken);

        if (status is null)
        {
            throw new NotFoundException($"El estado con id {id} no existe.");
        }

        if (await _catalog.StatusNameExistsAsync(_currentUser.CompanyId, request.Name, id, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Name", ["Ya existe un estado con ese nombre."] }
            });
        }

        status.Name = request.Name;
        status.SortOrder = request.SortOrder;
        status.IsClosed = request.IsClosed;
        status.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StatusDto { Id = status.Id, Name = status.Name, SortOrder = status.SortOrder, IsClosed = status.IsClosed, IsActive = status.IsActive };
    }
}
