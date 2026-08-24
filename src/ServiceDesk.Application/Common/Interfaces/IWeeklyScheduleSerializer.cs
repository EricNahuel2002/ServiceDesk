using ServiceDesk.Domain.Sla;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IWeeklyScheduleSerializer
{
    bool TryDeserialize(string? json, out WeeklySchedule? schedule);

    string Serialize(WeeklySchedule schedule);
}
