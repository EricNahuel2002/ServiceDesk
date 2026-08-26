namespace ServiceDesk.Domain.Sla;

public static class BusinessHoursCalculator
{
    public static bool IsBusinessHoursEnabled(CompanyBusinessHours? businessHours) =>
        businessHours is not null && businessHours.UseBusinessHours;

    public static bool IsWithinBusinessHours(
        DateTime utcNow,
        CompanyBusinessHours businessHours)
    {
        TimeZoneInfo timeZone = GetTimeZone(businessHours.TimeZoneId);
        DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);

        if (!businessHours.Schedule.TryGetEnabledWindow(localNow.DayOfWeek, out DayWindow window))
        {
            return false;
        }

        TimeOnly currentTime = TimeOnly.FromDateTime(localNow);

        return currentTime >= window.Start!.Value && currentTime < window.End!.Value;
    }

    public static int CalculateDelayMinutes(
        DateTime assignedAtUtc,
        DateTime? startedWorkAtUtc,
        int maxAssignmentToStartMinutes)
    {
        DateTime endTime = startedWorkAtUtc ?? DateTime.UtcNow;

        if (endTime <= assignedAtUtc)
        {
            return 0;
        }

        DateTime graceDeadline = assignedAtUtc.AddMinutes(maxAssignmentToStartMinutes);

        if (endTime <= graceDeadline)
        {
            return 0;
        }

        return (int)(endTime - graceDeadline).TotalMinutes;
    }

    public static TimeSpan CalculateElapsed(
        DateTime fromUtc,
        DateTime toUtc,
        CompanyBusinessHours businessHours)
    {
        if (fromUtc >= toUtc)
        {
            return TimeSpan.Zero;
        }

        TimeZoneInfo timeZone = GetTimeZone(businessHours.TimeZoneId);
        DateTime fromLocal = TimeZoneInfo.ConvertTimeFromUtc(fromUtc, timeZone);
        DateTime toLocal = TimeZoneInfo.ConvertTimeFromUtc(toUtc, timeZone);

        TimeSpan total = TimeSpan.Zero;
        DateTime current = fromLocal;

        while (current < toLocal)
        {
            if (businessHours.Schedule.TryGetEnabledWindow(current.DayOfWeek, out DayWindow window))
            {
                DateTime windowStart = current.Date.Add(window.Start!.Value.ToTimeSpan());
                DateTime windowEnd = current.Date.Add(window.End!.Value.ToTimeSpan());

                DateTime effectiveStart = current > windowStart ? current : windowStart;
                DateTime effectiveEnd = toLocal < windowEnd ? toLocal : windowEnd;

                if (effectiveStart < effectiveEnd)
                {
                    total += effectiveEnd - effectiveStart;
                }
            }

            current = current.Date.AddDays(1);
        }

        return total;
    }

    public static DateTime AddBusinessHours(
        DateTime fromUtc,
        int hoursToAdd,
        CompanyBusinessHours businessHours)
    {
        if (hoursToAdd <= 0 || !businessHours.Schedule.HasEnabledWindows)
        {
            return fromUtc;
        }

        TimeZoneInfo timeZone = GetTimeZone(businessHours.TimeZoneId);
        DateTime current = TimeZoneInfo.ConvertTimeFromUtc(fromUtc, timeZone);
        int remainingMinutes = hoursToAdd * 60;

        while (remainingMinutes > 0)
        {
            if (businessHours.Schedule.TryGetEnabledWindow(current.DayOfWeek, out DayWindow window))
            {
                DateTime windowStart = current.Date.Add(window.Start!.Value.ToTimeSpan());
                DateTime windowEnd = current.Date.Add(window.End!.Value.ToTimeSpan());
                DateTime effectiveStart = current > windowStart ? current : windowStart;

                if (effectiveStart < windowEnd)
                {
                    int availableMinutes = (int)(windowEnd - effectiveStart).TotalMinutes;
                    int minutesToUse = Math.Min(remainingMinutes, availableMinutes);

                    current = effectiveStart.AddMinutes(minutesToUse);
                    remainingMinutes -= minutesToUse;

                    if (remainingMinutes == 0)
                    {
                        break;
                    }
                }
            }

            do
            {
                current = current.Date.AddDays(1);
            }
            while (!businessHours.Schedule.TryGetEnabledWindow(current.DayOfWeek, out _));

            current = current.Date.Add(
                businessHours.Schedule.Days[current.DayOfWeek].Start!.Value.ToTimeSpan());
        }

        return TimeZoneInfo.ConvertTimeToUtc(current, timeZone);
    }

    public static decimal CalculatePercentageElapsed(
        DateTime fromUtc,
        DateTime toUtc,
        CompanyBusinessHours businessHours,
        int totalHoursLimit)
    {
        if (totalHoursLimit <= 0)
        {
            return 100m;
        }

        TimeSpan elapsed = CalculateElapsed(fromUtc, toUtc, businessHours);
        TimeSpan limit = TimeSpan.FromHours(totalHoursLimit);

        if (limit <= TimeSpan.Zero)
        {
            return 100m;
        }

        decimal percentage = (decimal)elapsed.TotalMinutes / (decimal)limit.TotalMinutes * 100m;

        return Math.Min(percentage, 100m);
    }

    private static TimeZoneInfo GetTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
