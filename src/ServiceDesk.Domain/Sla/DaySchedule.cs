namespace ServiceDesk.Domain.Sla;

public sealed class DaySchedule
{
    public bool Enabled { get; set; }

    public string? Start { get; set; }

    public string? End { get; set; }
}
