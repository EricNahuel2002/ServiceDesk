using ServiceDesk.Domain.Sla;
using ServiceDesk.Infrastructure.Persistence.Serialization;

namespace ServiceDesk.UnitTests.Infrastructure.Persistence.Serialization;

public class WeeklyScheduleJsonSerializerTests
{
    private readonly WeeklyScheduleJsonSerializer _serializer = new();

    [Fact]
    public void TryDeserialize_ParsesCanonicalJson()
    {
        string json = """
            {"Monday":{"enabled":true,"start":"08:00","end":"17:00"},"Tuesday":{"enabled":false}}
            """;

        bool result = _serializer.TryDeserialize(json, out WeeklySchedule? schedule);

        Assert.True(result);
        Assert.NotNull(schedule);
        Assert.True(schedule!.TryGetEnabledWindow(DayOfWeek.Monday, out DayWindow window));
        Assert.Equal(new TimeOnly(8, 0), window.Start);
        Assert.Equal(new TimeOnly(17, 0), window.End);
        Assert.False(schedule.TryGetEnabledWindow(DayOfWeek.Tuesday, out _));
    }

    [Fact]
    public void TryDeserialize_ParsesLegacyPascalCaseProperties()
    {
        string json = """
            {"Monday":{"Enabled":true,"Start":"08:00","End":"12:00"}}
            """;

        bool result = _serializer.TryDeserialize(json, out WeeklySchedule? schedule);

        Assert.True(result);
        Assert.True(schedule!.TryGetEnabledWindow(DayOfWeek.Monday, out _));
    }

    [Fact]
    public void TryDeserialize_ParsesSpanishDayKeys()
    {
        string json = """
            {"Lunes":{"enabled":true,"start":"09:00","end":"18:00"}}
            """;

        bool result = _serializer.TryDeserialize(json, out WeeklySchedule? schedule);

        Assert.True(result);
        Assert.True(schedule!.TryGetEnabledWindow(DayOfWeek.Monday, out _));
    }

    [Fact]
    public void TryDeserialize_FillsMissingDaysAsClosed()
    {
        string json = """
            {"Lunes":{"enabled":true,"start":"09:00","end":"18:00"}}
            """;

        bool result = _serializer.TryDeserialize(json, out WeeklySchedule? schedule);

        Assert.True(result);
        Assert.False(schedule!.TryGetEnabledWindow(DayOfWeek.Tuesday, out _));
        Assert.False(schedule.TryGetEnabledWindow(DayOfWeek.Sunday, out _));
    }

    [Fact]
    public void TryDeserialize_IgnoresUnknownDayKeys()
    {
        string json = """
            {"Funday":{"enabled":true,"start":"08:00","end":"10:00"},"Lunes":{"enabled":true,"start":"08:00","end":"10:00"}}
            """;

        bool result = _serializer.TryDeserialize(json, out WeeklySchedule? schedule);

        Assert.True(result);
        Assert.True(schedule!.TryGetEnabledWindow(DayOfWeek.Monday, out _));
        Assert.Equal(7, schedule.Days.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no es json")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{\"Lunes\":{\"enabled\":true,\"start\":\"8\",\"end\":\"17:00\"}}")]
    [InlineData("{\"Lunes\":{\"enabled\":true,\"start\":\"08:00\",\"end\":\"25:00\"}}")]
    [InlineData("{\"Lunes\":{\"enabled\":true}}")]
    [InlineData("{\"Lunes\":{\"enabled\":true,\"start\":\"18:00\",\"end\":\"08:00\"}}")]
    public void TryDeserialize_ReturnsFalse_ForInvalidInput(string? json)
    {
        bool result = _serializer.TryDeserialize(json, out WeeklySchedule? schedule);

        Assert.False(result);
        Assert.Null(schedule);
    }

    [Fact]
    public void Serialize_ProducesCanonicalJsonOrderedFromMondayToSunday()
    {
        const string expected =
            """
            {"Monday":{"enabled":true,"start":"08:00","end":"17:00"},"Tuesday":{"enabled":true,"start":"08:00","end":"17:00"},"Wednesday":{"enabled":true,"start":"08:00","end":"17:00"},"Thursday":{"enabled":true,"start":"08:00","end":"17:00"},"Friday":{"enabled":true,"start":"08:00","end":"17:00"},"Saturday":{"enabled":false},"Sunday":{"enabled":false}}
            """;

        string json = _serializer.Serialize(WeeklySchedule.CreateDefault());

        Assert.Equal(expected, json);
    }

    [Fact]
    public void Serialize_RoundTrip_PreservesSchedule()
    {
        WeeklySchedule original = WeeklySchedule.CreateDefault();

        bool result = _serializer.TryDeserialize(_serializer.Serialize(original), out WeeklySchedule? parsed);

        Assert.True(result);
        Assert.Equal(_serializer.Serialize(original), _serializer.Serialize(parsed!));
    }
}
