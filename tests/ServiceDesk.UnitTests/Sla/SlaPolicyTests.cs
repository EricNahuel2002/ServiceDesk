using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.UnitTests.Sla;

public sealed class SlaPolicyTests
{
    private static readonly DateTime CreatedAt = new(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

    private static Ticket CreateTicket(DateTime responseDeadline)
    {
        Ticket ticket = new()
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = CreatedAt,
            Priority = TicketPriority.Media,
            ResponseDeadlineAtUtc = responseDeadline
        };

        return ticket;
    }

    private static TicketSlaRecord CreateRecord(Ticket ticket, int slaLimitHours)
    {
        return new TicketSlaRecord
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Priority = ticket.Priority!.Value,
            SlaLimitHours = slaLimitHours,
            ResponseDeadlineAtUtc = ticket.ResponseDeadlineAtUtc,
            IsCurrent = true
        };
    }

    [Fact]
    public void IsExpiring_ReturnsTrue_WhenElapsedReachesThreshold()
    {
        Ticket ticket = CreateTicket(CreatedAt.AddHours(8));
        TicketSlaRecord record = CreateRecord(ticket, 8);

        DateTime now = CreatedAt.AddHours(6);

        Assert.True(SlaPolicy.IsExpiring(ticket, record, now));
    }

    [Fact]
    public void IsExpiring_ReturnsFalse_BeforeThreshold()
    {
        Ticket ticket = CreateTicket(CreatedAt.AddHours(8));
        TicketSlaRecord record = CreateRecord(ticket, 8);

        DateTime now = CreatedAt.AddHours(5).AddMinutes(59);

        Assert.False(SlaPolicy.IsExpiring(ticket, record, now));
    }

    [Fact]
    public void IsExpiring_ReturnsFalse_WhenWindowIsInvalid()
    {
        Ticket ticket = CreateTicket(CreatedAt);
        TicketSlaRecord record = CreateRecord(ticket, 8);

        Assert.False(SlaPolicy.IsExpiring(ticket, record, CreatedAt.AddHours(1)));
    }

    [Fact]
    public void IsBreached_ReturnsTrue_AfterDeadline()
    {
        Ticket ticket = CreateTicket(CreatedAt.AddHours(4));
        TicketSlaRecord record = CreateRecord(ticket, 4);

        Assert.True(SlaPolicy.IsBreached(record, CreatedAt.AddHours(4)));
    }

    [Fact]
    public void IsBreached_ReturnsFalse_BeforeDeadline()
    {
        Ticket ticket = CreateTicket(CreatedAt.AddHours(4));
        TicketSlaRecord record = CreateRecord(ticket, 4);

        Assert.False(SlaPolicy.IsBreached(record, CreatedAt.AddHours(3).AddMinutes(59)));
    }

    [Fact]
    public void MarkBreached_SetsBreachedAndGraceDeadline()
    {
        Ticket ticket = CreateTicket(CreatedAt.AddHours(4));
        TicketSlaRecord record = CreateRecord(ticket, 4);
        DateTime now = CreatedAt.AddHours(4).AddMinutes(1);

        SlaPolicy.MarkBreached(record, now);

        Assert.Equal(now, record.BreachedAtUtc);
        Assert.Equal(now.AddMinutes(SlaPolicy.GraceMinutes), record.GraceDeadlineUtc);
    }

    [Fact]
    public void MarkBreached_IsIdempotent()
    {
        Ticket ticket = CreateTicket(CreatedAt.AddHours(4));
        TicketSlaRecord record = CreateRecord(ticket, 4);
        DateTime firstRun = CreatedAt.AddHours(4).AddMinutes(1);

        SlaPolicy.MarkBreached(record, firstRun);
        SlaPolicy.MarkBreached(record, firstRun.AddMinutes(30));

        Assert.Equal(firstRun, record.BreachedAtUtc);
        Assert.Equal(firstRun.AddMinutes(SlaPolicy.GraceMinutes), record.GraceDeadlineUtc);
    }

    [Fact]
    public void IsGraceExceeded_ReturnsTrue_AfterGracePeriod()
    {
        Ticket ticket = CreateTicket(CreatedAt.AddHours(4));
        TicketSlaRecord record = CreateRecord(ticket, 4);
        DateTime breachedAt = CreatedAt.AddHours(4);

        SlaPolicy.MarkBreached(record, breachedAt);

        Assert.True(SlaPolicy.IsGraceExceeded(record, breachedAt.AddMinutes(61)));
    }

    [Fact]
    public void IsGraceExceeded_ReturnsFalse_WithinGracePeriod()
    {
        Ticket ticket = CreateTicket(CreatedAt.AddHours(4));
        TicketSlaRecord record = CreateRecord(ticket, 4);
        DateTime breachedAt = CreatedAt.AddHours(4);

        SlaPolicy.MarkBreached(record, breachedAt);

        Assert.False(SlaPolicy.IsGraceExceeded(record, breachedAt.AddMinutes(59)));
    }

    [Fact]
    public void IsGraceExceeded_ReturnsFalse_WhenNotBreached()
    {
        Ticket ticket = CreateTicket(CreatedAt.AddHours(4));
        TicketSlaRecord record = CreateRecord(ticket, 4);

        Assert.False(SlaPolicy.IsGraceExceeded(record, CreatedAt.AddHours(8)));
    }

    [Fact]
    public void MarkCanceled_SetsCanceledAndDeactivatesRecord()
    {
        Ticket ticket = CreateTicket(CreatedAt.AddHours(4));
        TicketSlaRecord record = CreateRecord(ticket, 4);
        DateTime now = CreatedAt.AddHours(5);

        SlaPolicy.MarkCanceled(record, now);

        Assert.Equal(now, record.CanceledAtUtc);
        Assert.False(record.IsCurrent);
    }
}