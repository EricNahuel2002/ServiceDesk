namespace ServiceDesk.Domain.Audit;

public static class TicketAuditActions
{
    public const string Created = "created";
    public const string Assigned = "assigned";
    public const string Reassigned = "reassigned";
    public const string WorkStarted = "work_started";
    public const string Resolved = "resolved";
    public const string Reopened = "reopened";
    public const string FeedbackSubmitted = "feedback_submitted";
    public const string TechnicianReport = "technician_report";
}
