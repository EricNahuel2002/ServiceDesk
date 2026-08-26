using ServiceDesk.Domain.Common;

namespace ServiceDesk.Domain.Sla;

public sealed class WeeklySchedule
{
    private WeeklySchedule(IReadOnlyDictionary<DayOfWeek, DayWindow> days) => Days = days;

    public IReadOnlyDictionary<DayOfWeek, DayWindow> Days { get; }

    public bool HasEnabledWindows => Days.Values.Any(day => day.Enabled);

    public bool TryGetEnabledWindow(DayOfWeek day, out DayWindow window)
    {
        if (Days.TryGetValue(day, out DayWindow candidate)
            && candidate.Enabled
            && candidate.Start.HasValue
            && candidate.End.HasValue)
        {
            window = candidate;
            return true;
        }

        window = default;
        return false;
    }

    public static WeeklySchedule Create(IReadOnlyDictionary<DayOfWeek, DayWindow> days)
    {
        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            if (!days.ContainsKey(day))
            {
                throw new DomainRuleViolationException($"Falta definir el horario del día {day}.");
            }
        }

        foreach (KeyValuePair<DayOfWeek, DayWindow> entry in days)
        {
            DayWindow window = entry.Value;

            if (!window.Enabled)
            {
                continue;
            }

            if (!window.Start.HasValue || !window.End.HasValue)
            {
                throw new DomainRuleViolationException(
                    $"El día {entry.Key} está habilitado pero no define hora de inicio y fin.");
            }

            if (window.Start.Value >= window.End.Value)
            {
                throw new DomainRuleViolationException(
                    $"El día {entry.Key} tiene una hora de inicio posterior o igual a la hora de fin.");
            }
        }

        return new WeeklySchedule(new Dictionary<DayOfWeek, DayWindow>(days));
    }

    public static WeeklySchedule CreateDefault() =>
        Create(new Dictionary<DayOfWeek, DayWindow>
        {
            [DayOfWeek.Monday] = new(true, new TimeOnly(8, 0), new TimeOnly(17, 0)),
            [DayOfWeek.Tuesday] = new(true, new TimeOnly(8, 0), new TimeOnly(17, 0)),
            [DayOfWeek.Wednesday] = new(true, new TimeOnly(8, 0), new TimeOnly(17, 0)),
            [DayOfWeek.Thursday] = new(true, new TimeOnly(8, 0), new TimeOnly(17, 0)),
            [DayOfWeek.Friday] = new(true, new TimeOnly(8, 0), new TimeOnly(17, 0)),
            [DayOfWeek.Saturday] = DayWindow.Closed(),
            [DayOfWeek.Sunday] = DayWindow.Closed()
        });
}
