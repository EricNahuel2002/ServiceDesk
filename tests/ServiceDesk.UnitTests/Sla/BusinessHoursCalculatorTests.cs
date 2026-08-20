using System.Globalization;
using System.Text.Json;
using ServiceDesk.Domain.Sla;

namespace ServiceDesk.UnitTests.Sla;

public class BusinessHoursCalculatorTests
{
    private static CompanyBusinessHours CreateBusinessHours(
        string timeZoneId,
        bool useBusinessHours,
        string businessHoursJson,
        int maxAssignmentToStartMinutes = 0)
    {
        return new CompanyBusinessHours
        {
            TimeZoneId = timeZoneId,
            UseBusinessHours = useBusinessHours,
            BusinessHoursJson = businessHoursJson,
            MaxAssignmentToStartMinutes = maxAssignmentToStartMinutes,
        };
    }

    private static string ToJson(string start = "08:00", string end = "15:00", bool enabled = true) =>
        JsonSerializer.Serialize(new
        {
            Monday = new { enabled, start, end },
            Tuesday = new { enabled, start, end },
            Wednesday = new { enabled, start, end },
            Thursday = new { enabled, start, end },
            Friday = new { enabled, start, end },
            Saturday = new { enabled = (bool)false, start = "00:00", end = "00:00" },
            Sunday = new { enabled = (bool)false, start = "00:00", end = "00:00" }
        });

    [Theory]
    [InlineData("2026-08-18T12:00:00Z", "America/New_York", true)]
    [InlineData("2026-08-18T18:00:00Z", "America/New_York", true)]
    [InlineData("2026-08-18T19:00:00Z", "America/New_York", false)]
    [InlineData("2026-08-22T12:00:00Z", "America/New_York", false)]
    [InlineData("2026-08-23T12:00:00Z", "America/New_York", false)]
    public void IsWithinBusinessHours_ReturnsExpected(
        string utcDateTimeString,
        string timeZoneId,
        bool expected)
    {
        CompanyBusinessHours businessHours = CreateBusinessHours(
            timeZoneId, true, ToJson("08:00", "15:00"));

        DateTime utcNow = DateTime.Parse(utcDateTimeString, null, DateTimeStyles.AdjustToUniversal);

        bool result = BusinessHoursCalculator.IsWithinBusinessHours(utcNow, businessHours);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsWithinBusinessHours_SkipsWhenUseBusinessHoursIsFalse()
    {
        CompanyBusinessHours businessHours = CreateBusinessHours(
            "America/New_York", false, ToJson());

        DateTime mondayAtNoonUtc = new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc);

        bool result = BusinessHoursCalculator.IsWithinBusinessHours(mondayAtNoonUtc, businessHours);

        Assert.True(result);
    }

    [Theory]
    [InlineData("2026-08-18T12:30:00Z", 0, 30)]
    [InlineData("2026-08-18T13:00:00Z", 0, 0)]
    [InlineData("2026-08-18T13:00:00Z", 30, 0)]
    [InlineData("2026-08-18T13:00:00Z", 60, 0)]
    public void CalculateDelayMinutes_ReturnsExpected(
        string assignedAtUtcString,
        int maxAssignmentToStartMinutes,
        int expectedDelay)
    {
        DateTime assignedAtUtc = DateTime.Parse(assignedAtUtcString, null, DateTimeStyles.AdjustToUniversal);
        DateTime startedWorkAtUtc = DateTime.Parse("2026-08-18T13:00:00Z", null, DateTimeStyles.AdjustToUniversal);

        int delay = BusinessHoursCalculator.CalculateDelayMinutes(
            assignedAtUtc,
            startedWorkAtUtc,
            maxAssignmentToStartMinutes);

        Assert.Equal(expectedDelay, delay);
    }

    [Fact]
    public void CalculateDelayMinutes_ReturnsZeroWhenStartedWithinGracePeriod()
    {
        DateTime assignedAtUtc = new DateTime(2026, 8, 18, 12, 30, 0, DateTimeKind.Utc);
        DateTime startedWorkAtUtc = new DateTime(2026, 8, 18, 12, 35, 0, DateTimeKind.Utc);

        int delay = BusinessHoursCalculator.CalculateDelayMinutes(
            assignedAtUtc, startedWorkAtUtc, maxAssignmentToStartMinutes: 120);

        Assert.Equal(0, delay);
    }

    [Fact]
    public void CalculateDelayMinutes_ReturnsFullElapsedMinutesWhenGraceIsZero()
    {
        DateTime assignedAtUtc = new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc);
        DateTime startedWorkAtUtc = new DateTime(2026, 8, 18, 12, 45, 0, DateTimeKind.Utc);

        int delay = BusinessHoursCalculator.CalculateDelayMinutes(
            assignedAtUtc, startedWorkAtUtc, maxAssignmentToStartMinutes: 0);

        Assert.Equal(45, delay);
    }

    [Fact]
    public void CalculateDelayMinutes_ReturnsDelayBeyondGracePeriod()
    {
        DateTime assignedAtUtc = new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc);
        DateTime startedWorkAtUtc = new DateTime(2026, 8, 18, 13, 00, 0, DateTimeKind.Utc);

        int delay = BusinessHoursCalculator.CalculateDelayMinutes(
            assignedAtUtc, startedWorkAtUtc, maxAssignmentToStartMinutes: 30);

        Assert.Equal(30, delay);
    }
}
