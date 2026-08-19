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

    public CatalogService(
        ICatalogRepository catalog,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IValidator<CreateCategoryRequest> createCategoryValidator,
        IValidator<UpdateCategoryRequest> updateCategoryValidator)
    {
        _catalog = catalog;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _createCategoryValidator = createCategoryValidator;
        _updateCategoryValidator = updateCategoryValidator;
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        _catalog.GetActiveCategoriesAsync(_currentUser.CompanyId, cancellationToken);

    public Task<IReadOnlyList<StatusDto>> GetStatusesAsync(CancellationToken cancellationToken) =>
        _catalog.GetActiveStatusesAsync(_currentUser.CompanyId, cancellationToken);

    public Task<IReadOnlyList<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken) =>
        _catalog.GetAllCategoriesAsync(_currentUser.CompanyId, cancellationToken);

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
}
