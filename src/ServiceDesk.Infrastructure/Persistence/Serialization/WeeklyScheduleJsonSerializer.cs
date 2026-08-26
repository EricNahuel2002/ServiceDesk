using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Sla;

namespace ServiceDesk.Infrastructure.Persistence.Serialization;

public sealed class WeeklyScheduleJsonSerializer : IWeeklyScheduleSerializer
{
    private const string TimeFormat = "HH:mm";

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly DayOfWeek[] OrderedDays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];

    private static readonly Dictionary<string, DayOfWeek> DayNames = BuildDayNames();

    public bool TryDeserialize(string? json, out WeeklySchedule? schedule)
    {
        schedule = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        Dictionary<string, SerializedDay>? rawDays;

        try
        {
            rawDays = JsonSerializer.Deserialize<Dictionary<string, SerializedDay>>(json, ReadOptions);
        }
        catch (JsonException)
        {
            return false;
        }

        if (rawDays is null || rawDays.Count == 0)
        {
            return false;
        }

        Dictionary<DayOfWeek, DayWindow> days = [];

        foreach (KeyValuePair<string, SerializedDay> entry in rawDays)
        {
            if (!DayNames.TryGetValue(entry.Key.Trim(), out DayOfWeek day))
            {
                continue;
            }

            if (!TryParseWindow(entry.Value, out DayWindow window))
            {
                return false;
            }

            days[day] = window;
        }

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            days.TryAdd(day, DayWindow.Closed());
        }

        try
        {
            schedule = WeeklySchedule.Create(days);
        }
        catch (DomainRuleViolationException)
        {
            return false;
        }

        return true;
    }

    public string Serialize(WeeklySchedule schedule)
    {
        Dictionary<string, CanonicalDay> days = new(7);

        foreach (DayOfWeek day in OrderedDays)
        {
            if (schedule.TryGetEnabledWindow(day, out DayWindow window))
            {
                days[day.ToString()] = new CanonicalDay
                {
                    Enabled = true,
                    Start = FormatTime(window.Start!.Value),
                    End = FormatTime(window.End!.Value)
                };
            }
            else
            {
                days[day.ToString()] = new CanonicalDay { Enabled = false };
            }
        }

        return JsonSerializer.Serialize(days, WriteOptions);
    }

    private static bool TryParseWindow(SerializedDay? day, out DayWindow window)
    {
        window = DayWindow.Closed();

        if (day is null || !day.Enabled)
        {
            return true;
        }

        if (day.Start is null
            || day.End is null
            || !TimeOnly.TryParseExact(day.Start, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly start)
            || !TimeOnly.TryParseExact(day.End, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly end))
        {
            return false;
        }

        window = new DayWindow(true, start, end);
        return true;
    }

    private static string FormatTime(TimeOnly time) =>
        time.ToString(TimeFormat, CultureInfo.InvariantCulture);

    private static Dictionary<string, DayOfWeek> BuildDayNames() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Monday"] = DayOfWeek.Monday,
            ["Tuesday"] = DayOfWeek.Tuesday,
            ["Wednesday"] = DayOfWeek.Wednesday,
            ["Thursday"] = DayOfWeek.Thursday,
            ["Friday"] = DayOfWeek.Friday,
            ["Saturday"] = DayOfWeek.Saturday,
            ["Sunday"] = DayOfWeek.Sunday,
            ["Lunes"] = DayOfWeek.Monday,
            ["Martes"] = DayOfWeek.Tuesday,
            ["Miércoles"] = DayOfWeek.Wednesday,
            ["Miercoles"] = DayOfWeek.Wednesday,
            ["Jueves"] = DayOfWeek.Thursday,
            ["Viernes"] = DayOfWeek.Friday,
            ["Sábado"] = DayOfWeek.Saturday,
            ["Sabado"] = DayOfWeek.Saturday,
            ["Domingo"] = DayOfWeek.Sunday
        };

    private sealed class SerializedDay
    {
        public bool Enabled { get; set; }

        public string? Start { get; set; }

        public string? End { get; set; }
    }

    private sealed class CanonicalDay
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("start")]
        public string? Start { get; set; }

        [JsonPropertyName("end")]
        public string? End { get; set; }
    }
}
