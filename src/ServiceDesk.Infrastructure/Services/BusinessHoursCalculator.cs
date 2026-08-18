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
}
