using FluentValidation;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Common.Validation;
using ServiceDesk.Application.DTOs.Sla;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Sla;
using ValidationException = ServiceDesk.Application.Common.Exceptions.ValidationException;

namespace ServiceDesk.Application.Features.Sla;

public sealed class SlaService : ISlaService
{
    private readonly ISlaRepository _slaRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateSlaConfigurationRequest> _slaValidator;
    private readonly IValidator<UpdateBusinessHoursRequest> _businessHoursValidator;

    public SlaService(
        ISlaRepository slaRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IValidator<UpdateSlaConfigurationRequest> slaValidator,
        IValidator<UpdateBusinessHoursRequest> businessHoursValidator)
    {
        _slaRepository = slaRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _slaValidator = slaValidator;
        _businessHoursValidator = businessHoursValidator;
    }

    public async Task<IReadOnlyList<SlaConfigurationDto>> GetSlaConfigurationsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<SlaConfiguration> configurations = await _slaRepository.GetByCompanyAsync(
            _currentUser.CompanyId,
            cancellationToken);

        return configurations.Select(c => new SlaConfigurationDto
        {
            Priority = c.Priority,
            ResponseTimeHours = c.ResponseTimeHours
        }).ToList();
    }

    public async Task<SlaConfigurationDto> UpdateSlaConfigurationAsync(
        UpdateSlaConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_slaValidator, request, cancellationToken);

        SlaConfiguration? existing = await _slaRepository.FindByCompanyAndPriorityAsync(
            _currentUser.CompanyId,
            request.Priority,
            cancellationToken);

        if (existing is null)
        {
            SlaConfiguration newConfig = new()
            {
                CompanyId = _currentUser.CompanyId,
                Priority = request.Priority,
                ResponseTimeHours = request.ResponseTimeHours
            };

            await _slaRepository.AddRangeAsync([newConfig], cancellationToken);
        }
        else
        {
            existing.ResponseTimeHours = request.ResponseTimeHours;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SlaConfigurationDto
        {
            Priority = request.Priority,
            ResponseTimeHours = request.ResponseTimeHours
        };
    }

    public async Task<BusinessHoursDto> GetBusinessHoursAsync(CancellationToken cancellationToken)
    {
        CompanyBusinessHours? businessHours = await _slaRepository.GetBusinessHoursAsync(
            _currentUser.CompanyId,
            cancellationToken);

        if (businessHours is null)
        {
            return new BusinessHoursDto
            {
                BusinessHoursJson = string.Empty,
                TimeZoneId = "UTC",
                UseBusinessHours = true,
                MaxAssignmentToStartMinutes = 0
            };
        }

        return new BusinessHoursDto
        {
            BusinessHoursJson = businessHours.BusinessHoursJson,
            TimeZoneId = businessHours.TimeZoneId,
            UseBusinessHours = businessHours.UseBusinessHours,
            MaxAssignmentToStartMinutes = businessHours.MaxAssignmentToStartMinutes
        };
    }

    public async Task<BusinessHoursDto> UpdateBusinessHoursAsync(
        UpdateBusinessHoursRequest request,
        CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_businessHoursValidator, request, cancellationToken);

        CompanyBusinessHours? existing = await _slaRepository.GetBusinessHoursAsync(
            _currentUser.CompanyId,
            cancellationToken);

        if (existing is null)
        {
            CompanyBusinessHours newBusinessHours = new()
            {
                CompanyId = _currentUser.CompanyId,
                BusinessHoursJson = request.BusinessHoursJson,
                TimeZoneId = request.TimeZoneId,
                UseBusinessHours = request.UseBusinessHours,
                MaxAssignmentToStartMinutes = request.MaxAssignmentToStartMinutes
            };

            await _slaRepository.AddAsync(newBusinessHours, cancellationToken);
        }
        else
        {
            existing.BusinessHoursJson = request.BusinessHoursJson;
            existing.TimeZoneId = request.TimeZoneId;
            existing.UseBusinessHours = request.UseBusinessHours;
            existing.MaxAssignmentToStartMinutes = request.MaxAssignmentToStartMinutes;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BusinessHoursDto
        {
            BusinessHoursJson = request.BusinessHoursJson,
            TimeZoneId = request.TimeZoneId,
            UseBusinessHours = request.UseBusinessHours,
            MaxAssignmentToStartMinutes = request.MaxAssignmentToStartMinutes
        };
    }
}
