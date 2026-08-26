using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Sla;

namespace ServiceDesk.UnitTests.Sla;

public class WeeklyScheduleTests
{
    [Fact]
    public void Create_Throws_WhenDayIsMissing()
    {
        Dictionary<DayOfWeek, DayWindow> days = BuildWeekdays();
        days.Remove(DayOfWeek.Sunday);

        Assert.Throws<DomainRuleViolationException>(() => WeeklySchedule.Create(days));
    }

    [Fact]
    public void Create_Throws_WhenEnabledDayHasNoTimes()
    {
        Dictionary<DayOfWeek, DayWindow> days = BuildWeekdays();
        days[DayOfWeek.Saturday] = new DayWindow(true, null, null);

        Assert.Throws<DomainRuleViolationException>(() => WeeklySchedule.Create(days));
    }

    [Fact]
    public void Create_Throws_WhenStartEqualsEnd()
    {
        Dictionary<DayOfWeek, DayWindow> days = BuildWeekdays();
        days[DayOfWeek.Monday] = new DayWindow(true, new TimeOnly(8, 0), new TimeOnly(8, 0));

        Assert.Throws<DomainRuleViolationException>(() => WeeklySchedule.Create(days));
    }

    [Fact]
    public void Create_Throws_WhenStartIsAfterEnd()
    {
        Dictionary<DayOfWeek, DayWindow> days = BuildWeekdays();
        days[DayOfWeek.Monday] = new DayWindow(true, new TimeOnly(18, 0), new TimeOnly(8, 0));

        Assert.Throws<DomainRuleViolationException>(() => WeeklySchedule.Create(days));
    }

    [Fact]
    public void Create_AcceptsValidSchedule()
    {
        WeeklySchedule schedule = WeeklySchedule.Create(BuildWeekdays());

        Assert.True(schedule.HasEnabledWindows);
        Assert.True(schedule.TryGetEnabledWindow(DayOfWeek.Monday, out DayWindow window));
        Assert.Equal(new TimeOnly(8, 0), window.Start);
        Assert.Equal(new TimeOnly(17, 0), window.End);
    }

    [Fact]
    public void TryGetEnabledWindow_ReturnsFalse_ForClosedDay()
    {
        WeeklySchedule schedule = WeeklySchedule.CreateDefault();

        Assert.False(schedule.TryGetEnabledWindow(DayOfWeek.Sunday, out _));
    }

    [Fact]
    public void HasEnabledWindows_ReturnsFalse_WhenAllDaysAreClosed()
    {
        Dictionary<DayOfWeek, DayWindow> days = Enum.GetValues<DayOfWeek>()
            .ToDictionary(day => day, _ => DayWindow.Closed());

        WeeklySchedule schedule = WeeklySchedule.Create(days);

        Assert.False(schedule.HasEnabledWindows);
    }

    [Fact]
    public void CreateDefault_DisablesWeekend_AndEnablesWeekdaysFrom8To17()
    {
        WeeklySchedule schedule = WeeklySchedule.CreateDefault();

        Assert.True(schedule.TryGetEnabledWindow(DayOfWeek.Monday, out DayWindow monday));
        Assert.Equal(new TimeOnly(8, 0), monday.Start);
        Assert.Equal(new TimeOnly(17, 0), monday.End);

        Assert.True(schedule.TryGetEnabledWindow(DayOfWeek.Friday, out _));
        Assert.False(schedule.TryGetEnabledWindow(DayOfWeek.Saturday, out _));
        Assert.False(schedule.TryGetEnabledWindow(DayOfWeek.Sunday, out _));
    }

    private static Dictionary<DayOfWeek, DayWindow> BuildWeekdays() =>
        Enum.GetValues<DayOfWeek>()
            .ToDictionary(
                day => day,
                day => day is DayOfWeek.Saturday or DayOfWeek.Sunday
                    ? DayWindow.Closed()
                    : new DayWindow(true, new TimeOnly(8, 0), new TimeOnly(17, 0)));
}
