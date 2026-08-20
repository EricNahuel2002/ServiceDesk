namespace ServiceDesk.Domain.Tickets;

public enum SlaRecordCancelReason
{
    SlaGraceExceeded = 0,

    AssignmentStartGraceExceeded = 1
}