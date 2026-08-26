namespace ServiceDesk.Domain.Audit;

public enum TicketAuditAction : byte
{
    Created = 1,
    Assigned = 2,
    Reassigned = 3,
    WorkStarted = 4,
    Resolved = 5,
    Reopened = 6,
    FeedbackSubmitted = 7,
    TechnicianReport = 8
}