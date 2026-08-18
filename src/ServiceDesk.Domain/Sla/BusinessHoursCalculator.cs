using System.Text.Json;

namespace ServiceDesk.Domain.Sla;

public static class BusinessHoursCalculator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool IsBusinessHoursEnabled(CompanyBusinessHours? businessHours) =>
        businessHours is not null && businessHours.UseBusinessHours;

    public static TimeSpan CalculateElapsed(
        DateTime fromUtc,
        DateTime toUtc,
        CompanyBusinessHours businessHours)
    {
        if (fromUtc >= toUtc)
        {
            return TimeSpan.Zero;
        }

        TimeZoneInfo tz = GetTimeZone(businessHours.TimeZoneId);
        Dictionary<string, DaySchedule> schedule = ParseSchedule(businessHours.BusinessHoursJson);

        DateTime fromLocal = TimeZoneInfo.ConvertTimeFromUtc(fromUtc, tz);
        DateTime toLocal = TimeZoneInfo.ConvertTimeFromUtc(toUtc, tz);

        TimeSpan total = TimeSpan.Zero;
        DateTime current = fromLocal;

        while (current < toLocal)
        {
            string dayName = current.DayOfWeek.ToString();
            string dayKey = GetDayKey(dayName);

            if (schedule.TryGetValue(dayKey, out DaySchedule? day) && day.Enabled && day.Start is not null && day.End is not null)
            {
                TimeOnly dayStart = TimeOnly.Parse(day.Start);
                TimeOnly dayEnd = TimeOnly.Parse(day.End);
                DateTime dayStartDateTime = current.Date.Add(dayStart.ToTimeSpan());
                DateTime dayEndDateTime = current.Date.Add(dayEnd.ToTimeSpan());

                DateTime windowStart = current > dayStartDateTime ? current : dayStartDateTime;
                DateTime windowEnd = toLocal < dayEndDateTime ? toLocal : dayEndDateTime;

                if (windowStart < windowEnd)
                {
                    total += windowEnd - windowStart;
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
        if (hoursToAdd <= 0)
        {
            return fromUtc;
        }

        TimeZoneInfo tz = GetTimeZone(businessHours.TimeZoneId);
        Dictionary<string, DaySchedule> schedule = ParseSchedule(businessHours.BusinessHoursJson);

        DateTime fromLocal = TimeZoneInfo.ConvertTimeFromUtc(fromUtc, tz);
        DateTime current = fromLocal;
        int remainingMinutes = hoursToAdd * 60;

        while (remainingMinutes > 0)
        {
            string dayName = current.DayOfWeek.ToString();
            string dayKey = GetDayKey(dayName);

            if (schedule.TryGetValue(dayKey, out DaySchedule? day) && day.Enabled && day.Start is not null && day.End is not null)
            {
                TimeOnly dayStart = TimeOnly.Parse(day.Start);
                TimeOnly dayEnd = TimeOnly.Parse(day.End);
                DateTime dayStartDateTime = current.Date.Add(dayStart.ToTimeSpan());
                DateTime dayEndDateTime = current.Date.Add(dayEnd.ToTimeSpan());

                DateTime windowStart = current > dayStartDateTime ? current : dayStartDateTime;

                if (windowStart < dayEndDateTime)
                {
                    int availableMinutes = (int)(dayEndDateTime - windowStart).TotalMinutes;
                    int minutesToUse = Math.Min(remainingMinutes, availableMinutes);
                    current = windowStart.AddMinutes(minutesToUse);
                    remainingMinutes -= minutesToUse;
                }
            }

            if (remainingMinutes > 0)
            {
                current = current.Date.AddDays(1);
                string nextDayName = current.DayOfWeek.ToString();
                string nextDayKey = GetDayKey(nextDayName);

                if (schedule.TryGetValue(nextDayKey, out DaySchedule? nextDay) && nextDay.Enabled && nextDay.Start is not null)
                {
                    TimeOnly nextDayStart = TimeOnly.Parse(nextDay.Start);
                    current = current.Date.Add(nextDayStart.ToTimeSpan());
                }
            }
        }

        return TimeZoneInfo.ConvertTimeToUtc(current, tz);
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

    private static Dictionary<string, DaySchedule> ParseSchedule(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return CreateDefaultSchedule();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, DaySchedule>>(json, JsonOptions)
                   ?? CreateDefaultSchedule();
        }
        catch
        {
            return CreateDefaultSchedule();
        }
    }

    private static Dictionary<string, DaySchedule> CreateDefaultSchedule() =>
        new()
        {
            ["Monday"] = new DaySchedule { Enabled = true, Start = "08:00", End = "17:00" },
            ["Tuesday"] = new DaySchedule { Enabled = true, Start = "08:00", End = "17:00" },
            ["Wednesday"] = new DaySchedule { Enabled = true, Start = "08:00", End = "17:00" },
            ["Thursday"] = new DaySchedule { Enabled = true, Start = "08:00", End = "17:00" },
            ["Friday"] = new DaySchedule { Enabled = true, Start = "08:00", End = "17:00" },
            ["Saturday"] = new DaySchedule { Enabled = false },
            ["Sunday"] = new DaySchedule { Enabled = false }
        };

    private static string GetDayKey(string dayName) =>
        dayName switch
        {
            "Lunes" => "Monday",
            "Martes" => "Tuesday",
            "Miércoles" => "Wednesday",
            "Jueves" => "Thursday",
            "Viernes" => "Friday",
            "Sábado" => "Saturday",
            "Domingo" => "Sunday",
            _ => dayName
        };
}
