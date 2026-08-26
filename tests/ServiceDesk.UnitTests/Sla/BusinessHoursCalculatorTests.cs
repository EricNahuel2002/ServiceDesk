using System.Globalization;
using ServiceDesk.Domain.Sla;

namespace ServiceDesk.UnitTests.Sla;

public class BusinessHoursCalculatorTests
{
    private static CompanyBusinessHours CreateBusinessHours(
        string timeZoneId,
        bool useBusinessHours,
        WeeklySchedule schedule,
        int maxAssignmentToStartMinutes = 0)
    {
        return new CompanyBusinessHours
        {
            TimeZoneId = timeZoneId,
            UseBusinessHours = useBusinessHours,
            Schedule = schedule,
            MaxAssignmentToStartMinutes = maxAssignmentToStartMinutes,
        };
    }

    private static WeeklySchedule WeekdaySchedule(TimeOnly start, TimeOnly end) =>
        WeeklySchedule.Create(new Dictionary<DayOfWeek, DayWindow>
        {
            [DayOfWeek.Monday] = new(true, start, end),
            [DayOfWeek.Tuesday] = new(true, start, end),
            [DayOfWeek.Wednesday] = new(true, start, end),
            [DayOfWeek.Thursday] = new(true, start, end),
            [DayOfWeek.Friday] = new(true, start, end),
            [DayOfWeek.Saturday] = DayWindow.Closed(),
            [DayOfWeek.Sunday] = DayWindow.Closed()
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
            timeZoneId, true, WeekdaySchedule(new TimeOnly(8, 0), new TimeOnly(15, 0)));

        DateTime utcNow = DateTime.Parse(utcDateTimeString, null, DateTimeStyles.AdjustToUniversal);

        bool result = BusinessHoursCalculator.IsWithinBusinessHours(utcNow, businessHours);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsWithinBusinessHours_SkipsWhenUseBusinessHoursIsFalse()
    {
        CompanyBusinessHours businessHours = CreateBusinessHours(
            "America/New_York", false, WeekdaySchedule(new TimeOnly(8, 0), new TimeOnly(15, 0)));

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

    [Theory]
    [InlineData("2026-08-17T10:00:00Z", "2026-08-17T12:00:00Z", 120)]
    [InlineData("2026-08-17T16:00:00Z", "2026-08-18T10:00:00Z", 180)]
    [InlineData("2026-08-17T07:00:00Z", "2026-08-17T08:30:00Z", 30)]
    public void CalculateElapsed_SumsOnlyBusinessMinutes(
        string fromUtcString,
        string toUtcString,
        int expectedMinutes)
    {
        CompanyBusinessHours businessHours = CreateUtcBusinessHours();

        TimeSpan elapsed = BusinessHoursCalculator.CalculateElapsed(
            ParseUtc(fromUtcString), ParseUtc(toUtcString), businessHours);

        Assert.Equal(expectedMinutes, elapsed.TotalMinutes);
    }

    [Fact]
    public void CalculateElapsed_ReturnsZero_WhenEntireRangeIsOutsideBusinessHours()
    {
        CompanyBusinessHours businessHours = CreateUtcBusinessHours();
        DateTime saturday = ParseUtc("2026-08-22T10:00:00Z");
        DateTime sunday = ParseUtc("2026-08-23T10:00:00Z");

        TimeSpan elapsed = BusinessHoursCalculator.CalculateElapsed(saturday, sunday, businessHours);

        Assert.Equal(TimeSpan.Zero, elapsed);
    }

    [Fact]
    public void IsWithinBusinessHours_IncludesStartAndExcludesEnd()
    {
        CompanyBusinessHours businessHours = CreateUtcBusinessHours();
        DateTime mondayAtStart = ParseUtc("2026-08-17T08:00:00Z");
        DateTime mondayAtEnd = ParseUtc("2026-08-17T17:00:00Z");

        Assert.True(BusinessHoursCalculator.IsWithinBusinessHours(mondayAtStart, businessHours));
        Assert.False(BusinessHoursCalculator.IsWithinBusinessHours(mondayAtEnd, businessHours));
    }

    [Fact]
    public void AddBusinessHours_LandsSameDay_WhenThereIsEnoughRoom()
    {
        CompanyBusinessHours businessHours = CreateUtcBusinessHours();
        DateTime mondayAt10 = ParseUtc("2026-08-17T10:00:00Z");

        DateTime result = BusinessHoursCalculator.AddBusinessHours(mondayAt10, 2, businessHours);

        Assert.Equal(ParseUtc("2026-08-17T12:00:00Z"), result);
    }

    [Fact]
    public void AddBusinessHours_CarriesOverToNextBusinessDay()
    {
        CompanyBusinessHours businessHours = CreateUtcBusinessHours();
        DateTime fridayAt16 = ParseUtc("2026-08-21T16:00:00Z");

        DateTime result = BusinessHoursCalculator.AddBusinessHours(fridayAt16, 2, businessHours);

        Assert.Equal(ParseUtc("2026-08-24T09:00:00Z"), result);
    }

    [Fact]
    public void AddBusinessHours_StartsAtWindowOpening_WhenArrivalIsBeforeOpening()
    {
        CompanyBusinessHours businessHours = CreateUtcBusinessHours();
        DateTime sundayNight = ParseUtc("2026-08-23T20:00:00Z");

        DateTime result = BusinessHoursCalculator.AddBusinessHours(sundayNight, 1, businessHours);

        Assert.Equal(ParseUtc("2026-08-24T09:00:00Z"), result);
    }

    [Fact]
    public void AddBusinessHours_ReturnsFromUtc_WhenNoWindowsAreEnabled()
    {
        Dictionary<DayOfWeek, DayWindow> allClosed = Enum.GetValues<DayOfWeek>()
            .ToDictionary(day => day, _ => DayWindow.Closed());
        CompanyBusinessHours businessHours = CreateBusinessHours("UTC", true, WeeklySchedule.Create(allClosed));
        DateTime from = ParseUtc("2026-08-17T10:00:00Z");

        DateTime result = BusinessHoursCalculator.AddBusinessHours(from, 4, businessHours);

        Assert.Equal(from, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddBusinessHours_ReturnsFromUtc_WhenHoursToAddIsNotPositive(int hoursToAdd)
    {
        CompanyBusinessHours businessHours = CreateUtcBusinessHours();
        DateTime mondayAt10 = ParseUtc("2026-08-17T10:00:00Z");

        DateTime result = BusinessHoursCalculator.AddBusinessHours(mondayAt10, hoursToAdd, businessHours);

        Assert.Equal(mondayAt10, result);
    }

    [Fact]
    public void CalculatePercentageElapsed_ComputesPercentageOfBusinessTime()
    {
        CompanyBusinessHours businessHours = CreateUtcBusinessHours();
        DateTime from = ParseUtc("2026-08-17T10:00:00Z");
        DateTime to = ParseUtc("2026-08-17T10:30:00Z");

        decimal percentage = BusinessHoursCalculator.CalculatePercentageElapsed(from, to, businessHours, totalHoursLimit: 1);

        Assert.Equal(50m, percentage);
    }

    [Fact]
    public void CalculatePercentageElapsed_CapsAt100()
    {
        CompanyBusinessHours businessHours = CreateUtcBusinessHours();
        DateTime from = ParseUtc("2026-08-17T08:00:00Z");
        DateTime to = ParseUtc("2026-08-17T15:00:00Z");

        decimal percentage = BusinessHoursCalculator.CalculatePercentageElapsed(from, to, businessHours, totalHoursLimit: 1);

        Assert.Equal(100m, percentage);
    }

    private static CompanyBusinessHours CreateUtcBusinessHours() =>
        CreateBusinessHours("UTC", true, WeekdaySchedule(new TimeOnly(8, 0), new TimeOnly(17, 0)));

    private static DateTime ParseUtc(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
}
