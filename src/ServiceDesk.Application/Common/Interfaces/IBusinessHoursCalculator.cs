using ServiceDesk.Domain.Sla;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IBusinessHoursCalculator
{
    TimeSpan CalculateElapsed(DateTime fromUtc, DateTime toUtc, CompanyBusinessHours businessHours);

    DateTime AddBusinessHours(DateTime fromUtc, int hoursToAdd, CompanyBusinessHours businessHours);

    decimal CalculatePercentageElapsed(
        DateTime fromUtc,
        DateTime toUtc,
        CompanyBusinessHours businessHours,
        int totalHoursLimit);

    bool IsWithinBusinessHours(DateTime utcNow, CompanyBusinessHours businessHours);

    int CalculateDelayMinutes(
        DateTime assignedAtUtc,
        DateTime? startedWorkAtUtc,
        int maxAssignmentToStartMinutes);
}
