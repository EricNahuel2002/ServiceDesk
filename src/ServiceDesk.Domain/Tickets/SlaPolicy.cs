namespace ServiceDesk.Domain.Tickets;

public static class SlaPolicy
{
    public const int GraceMinutes = 60;

    public const decimal ExpiringThresholdPercentage = 75m;

    public static bool IsExpiring(Ticket ticket, TicketSlaRecord record, DateTime utcNow)
    {
        if (!HasValidSlaWindow(ticket, record))
        {
            return false;
        }

        TimeSpan window = record.ResponseDeadlineAtUtc - ticket.CreatedAtUtc;

        if (window <= TimeSpan.Zero)
        {
            return false;
        }

        TimeSpan elapsed = utcNow - ticket.CreatedAtUtc;
        decimal percentage = (decimal)elapsed.TotalMinutes / (decimal)window.TotalMinutes * 100m;

        return percentage >= ExpiringThresholdPercentage;
    }

    public static bool IsBreached(TicketSlaRecord record, DateTime utcNow) =>
        utcNow >= record.ResponseDeadlineAtUtc;

    public static void MarkBreached(TicketSlaRecord record, DateTime utcNow)
    {
        if (record.BreachedAtUtc is not null)
        {
            return;
        }

        record.BreachedAtUtc = utcNow;
        record.GraceDeadlineUtc = utcNow.AddMinutes(GraceMinutes);
    }

    public static bool IsGraceExceeded(TicketSlaRecord record, DateTime utcNow) =>
        record.BreachedAtUtc is not null
        && record.GraceDeadlineUtc is not null
        && utcNow >= record.GraceDeadlineUtc.Value;

    public static void MarkCanceled(TicketSlaRecord record, DateTime utcNow)
    {
        record.CanceledAtUtc = utcNow;
        record.IsCurrent = false;
    }

    private static bool HasValidSlaWindow(Ticket ticket, TicketSlaRecord record) =>
        record.ResponseDeadlineAtUtc > ticket.CreatedAtUtc;
}