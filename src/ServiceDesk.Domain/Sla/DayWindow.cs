namespace ServiceDesk.Domain.Sla;

public readonly record struct DayWindow(bool Enabled, TimeOnly? Start, TimeOnly? End)
{
    public static DayWindow Closed() => new(false, null, null);
}
