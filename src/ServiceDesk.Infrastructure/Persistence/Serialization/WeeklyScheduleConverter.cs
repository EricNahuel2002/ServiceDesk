using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ServiceDesk.Domain.Sla;

namespace ServiceDesk.Infrastructure.Persistence.Serialization;

public sealed class WeeklyScheduleConverter : ValueConverter<WeeklySchedule, string>
{
    private static readonly WeeklyScheduleJsonSerializer Serializer = new();

    public WeeklyScheduleConverter()
        : base(
            schedule => Serialize(schedule),
            json => DeserializeOrDefault(json))
    {
    }

    private static string Serialize(WeeklySchedule schedule) =>
        Serializer.Serialize(schedule);

    private static WeeklySchedule DeserializeOrDefault(string json) =>
        Serializer.TryDeserialize(json, out WeeklySchedule? schedule)
            ? schedule!
            : WeeklySchedule.CreateDefault();
}
