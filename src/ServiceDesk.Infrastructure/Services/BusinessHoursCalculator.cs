using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Sla;

namespace ServiceDesk.Infrastructure.Services;

public sealed class BusinessHoursCalculator : IBusinessHoursCalculator
{
    public TimeSpan CalculateElapsed(DateTime fromUtc, DateTime toUtc, CompanyBusinessHours businessHours) =>
        Domain.Sla.BusinessHoursCalculator.CalculateElapsed(fromUtc, toUtc, businessHours);

    public DateTime AddBusinessHours(DateTime fromUtc, int hoursToAdd, CompanyBusinessHours businessHours) =>
        Domain.Sla.BusinessHoursCalculator.AddBusinessHours(fromUtc, hoursToAdd, businessHours);

    public decimal CalculatePercentageElapsed(
        DateTime fromUtc,
        DateTime toUtc,
        CompanyBusinessHours businessHours,
        int totalHoursLimit) =>
        Domain.Sla.BusinessHoursCalculator.CalculatePercentageElapsed(fromUtc, toUtc, businessHours, totalHoursLimit);

    public bool IsWithinBusinessHours(DateTime utcNow, CompanyBusinessHours businessHours) =>
        Domain.Sla.BusinessHoursCalculator.IsWithinBusinessHours(utcNow, businessHours);

    public int CalculateDelayMinutes(
        DateTime assignedAtUtc,
        DateTime? startedWorkAtUtc,
        int maxAssignmentToStartMinutes) =>
        Domain.Sla.BusinessHoursCalculator.CalculateDelayMinutes(assignedAtUtc, startedWorkAtUtc, maxAssignmentToStartMinutes);
}
