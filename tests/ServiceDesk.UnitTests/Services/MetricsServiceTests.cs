using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Metrics;
using ServiceDesk.Application.Features.Metrics;
using ServiceDesk.Domain.Enums;
using ServiceDesk.UnitTests.Fakes;

namespace ServiceDesk.UnitTests.Services;

public sealed class MetricsServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid TechnicianA = Guid.NewGuid();
    private static readonly Guid TechnicianB = Guid.NewGuid();

    [Fact]
    public async Task GetAdminMetricsAsync_EmptyData_ReturnsZeros()
    {
        MetricsService service = CreateService([]);

        AdminMetricsDto result = await service.GetAdminMetricsAsync(null, null, null, CancellationToken.None);

        Assert.Equal(0, result.TotalTickets);
        Assert.Equal(0, result.OpenTickets);
        Assert.Equal(0, result.InProgressTickets);
        Assert.Equal(0, result.ResolvedTickets);
        Assert.Equal(0, result.OverdueTickets);
        Assert.Equal(100, result.SlaCompliancePercentage);
        Assert.Empty(result.ByPriority);
        Assert.Empty(result.DailyTrend);
        Assert.Empty(result.ByTechnician);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_FiltersByTechnicianId()
    {
        DateTime now = DateTime.UtcNow;
        List<TicketMetricsRecord> tickets =
        [
            CreateTicket(TicketPriority.Alta, TechnicianA, "Nuevo", now.AddDays(-2)),
            CreateTicket(TicketPriority.Media, TechnicianB, "Nuevo", now.AddDays(-1)),
            CreateTicket(TicketPriority.Baja, TechnicianA, "Resuelto", now.AddDays(-3),
                startedAt: now.AddDays(-3), resolvedAt: now.AddDays(-2)),
        ];

        MetricsService service = CreateService(tickets);

        AdminMetricsDto result = await service.GetAdminMetricsAsync(
            null, null, TechnicianA, CancellationToken.None);

        Assert.Equal(2, result.TotalTickets);
        Assert.Equal(1, result.ResolvedTickets);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_FiltersByDateRange()
    {
        DateTime now = DateTime.UtcNow;
        List<TicketMetricsRecord> tickets =
        [
            CreateTicket(TicketPriority.Alta, TechnicianA, "Nuevo", now.AddDays(-10)),
            CreateTicket(TicketPriority.Media, TechnicianA, "Nuevo", now.AddDays(-2)),
            CreateTicket(TicketPriority.Baja, TechnicianA, "Nuevo", now),
        ];

        MetricsService service = CreateService(tickets);

        DateOnly from = DateOnly.FromDateTime(now.AddDays(-5));
        DateOnly to = DateOnly.FromDateTime(now);

        AdminMetricsDto result = await service.GetAdminMetricsAsync(
            from, to, null, CancellationToken.None);

        Assert.Equal(2, result.TotalTickets);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_ResolvedAfterDeadline_IsOverdue()
    {
        DateTime now = DateTime.UtcNow;
        DateTime deadline = now.AddHours(-2);
        List<TicketMetricsRecord> tickets =
        [
            CreateTicket(TicketPriority.Alta, TechnicianA, "Resuelto", now.AddHours(-10),
                startedAt: now.AddHours(-10), resolvedAt: now.AddHours(-1),
                responseDeadline: deadline),
        ];

        MetricsService service = CreateService(tickets);

        AdminMetricsDto result = await service.GetAdminMetricsAsync(
            null, null, null, CancellationToken.None);

        Assert.Equal(1, result.OverdueTickets);
        Assert.Equal(0, result.SlaCompliancePercentage);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_InProgressOverdue_IsOverdue()
    {
        DateTime now = DateTime.UtcNow;
        DateTime deadline = now.AddHours(-5);
        List<TicketMetricsRecord> tickets =
        [
            CreateTicket(TicketPriority.Critica, TechnicianA, "En Progreso", now.AddHours(-10),
                startedAt: now.AddHours(-10),
                responseDeadline: deadline),
        ];

        MetricsService service = CreateService(tickets);

        AdminMetricsDto result = await service.GetAdminMetricsAsync(
            null, null, null, CancellationToken.None);

        Assert.Equal(1, result.TotalTickets);
        Assert.Equal(1, result.InProgressTickets);
        Assert.Equal(1, result.OverdueTickets);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_ResolvedOnTime_IsNotOverdue()
    {
        DateTime now = DateTime.UtcNow;
        List<TicketMetricsRecord> tickets =
        [
            CreateTicket(TicketPriority.Alta, TechnicianA, "Resuelto", now.AddDays(-5),
                startedAt: now.AddDays(-5), resolvedAt: now.AddDays(-4),
                responseDeadline: now.AddDays(-3)),
        ];

        MetricsService service = CreateService(tickets);

        AdminMetricsDto result = await service.GetAdminMetricsAsync(
            null, null, null, CancellationToken.None);

        Assert.Equal(0, result.OverdueTickets);
        Assert.Equal(100, result.SlaCompliancePercentage);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_CancelledNotCountedAsResolved()
    {
        DateTime now = DateTime.UtcNow;
        List<TicketMetricsRecord> tickets =
        [
            CreateTicket(TicketPriority.Baja, TechnicianA, "Cancelado", now.AddDays(-2)),
            CreateTicket(TicketPriority.Baja, TechnicianA, "Resuelto", now.AddDays(-3),
                startedAt: now.AddDays(-3), resolvedAt: now.AddDays(-2)),
        ];

        MetricsService service = CreateService(tickets);

        AdminMetricsDto result = await service.GetAdminMetricsAsync(
            null, null, null, CancellationToken.None);

        Assert.Equal(2, result.TotalTickets);
        Assert.Equal(1, result.ResolvedTickets);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_ComputesAverageResolutionHours()
    {
        DateTime now = DateTime.UtcNow;
        List<TicketMetricsRecord> tickets =
        [
            CreateTicket(TicketPriority.Alta, TechnicianA, "Resuelto", now.AddDays(-5),
                startedAt: now.AddDays(-5), resolvedAt: now.AddDays(-5).AddHours(2)),
            CreateTicket(TicketPriority.Alta, TechnicianA, "Resuelto", now.AddDays(-4),
                startedAt: now.AddDays(-4), resolvedAt: now.AddDays(-4).AddHours(4)),
        ];

        MetricsService service = CreateService(tickets);

        AdminMetricsDto result = await service.GetAdminMetricsAsync(
            null, null, null, CancellationToken.None);

        Assert.Equal(3.0m, result.AverageResolutionHours);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_GroupsByPriority()
    {
        DateTime now = DateTime.UtcNow;
        List<TicketMetricsRecord> tickets =
        [
            CreateTicket(TicketPriority.Alta, TechnicianA, "Nuevo", now.AddDays(-1)),
            CreateTicket(TicketPriority.Alta, TechnicianA, "Nuevo", now.AddDays(-1)),
            CreateTicket(TicketPriority.Baja, TechnicianA, "Nuevo", now.AddDays(-1)),
        ];

        MetricsService service = CreateService(tickets);

        AdminMetricsDto result = await service.GetAdminMetricsAsync(
            null, null, null, CancellationToken.None);

        Assert.Equal(2, result.ByPriority.Count);

        PriorityMetricDto alta = result.ByPriority.First(p => p.Priority == TicketPriority.Alta);
        Assert.Equal(2, alta.Count);

        PriorityMetricDto baja = result.ByPriority.First(p => p.Priority == TicketPriority.Baja);
        Assert.Equal(1, baja.Count);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_GroupsByTechnician()
    {
        DateTime now = DateTime.UtcNow;
        List<TicketMetricsRecord> tickets =
        [
            CreateTicket(TicketPriority.Alta, TechnicianA, "Resuelto", now.AddDays(-3),
                startedAt: now.AddDays(-3), resolvedAt: now.AddDays(-2)),
            CreateTicket(TicketPriority.Media, TechnicianA, "Nuevo", now.AddDays(-1)),
            CreateTicket(TicketPriority.Baja, TechnicianB, "Nuevo", now),
        ];

        MetricsService service = CreateService(tickets);

        AdminMetricsDto result = await service.GetAdminMetricsAsync(
            null, null, null, CancellationToken.None);

        Assert.Equal(2, result.ByTechnician.Count);

        TechnicianMetricDto techA = result.ByTechnician.First(t => t.UserId == TechnicianA);
        Assert.Equal(2, techA.AssignedCount);
        Assert.Equal(1, techA.ResolvedCount);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_GroupsByDay()
    {
        DateTime today = DateTime.UtcNow;
        DateTime yesterday = today.AddDays(-1);
        List<TicketMetricsRecord> tickets =
        [
            CreateTicket(TicketPriority.Alta, TechnicianA, "Nuevo", yesterday),
            CreateTicket(TicketPriority.Media, TechnicianA, "Nuevo", yesterday),
            CreateTicket(TicketPriority.Baja, TechnicianA, "Nuevo", today),
        ];

        MetricsService service = CreateService(tickets);

        AdminMetricsDto result = await service.GetAdminMetricsAsync(
            null, null, null, CancellationToken.None);

        Assert.Equal(2, result.DailyTrend.Count);

        DailyMetricDto yesterdayMetric = result.DailyTrend.First(d => d.Date == DateOnly.FromDateTime(yesterday));
        Assert.Equal(2, yesterdayMetric.Created);

        DailyMetricDto todayMetric = result.DailyTrend.First(d => d.Date == DateOnly.FromDateTime(today));
        Assert.Equal(1, todayMetric.Created);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_OpenTicketNotStarted_IsOpen()
    {
        DateTime now = DateTime.UtcNow;
        List<TicketMetricsRecord> tickets =
        [
            CreateTicket(TicketPriority.Alta, TechnicianA, "Nuevo", now.AddDays(-1)),
        ];

        MetricsService service = CreateService(tickets);

        AdminMetricsDto result = await service.GetAdminMetricsAsync(
            null, null, null, CancellationToken.None);

        Assert.Equal(1, result.OpenTickets);
        Assert.Equal(0, result.InProgressTickets);
    }

    private static MetricsService CreateService(List<TicketMetricsRecord> tickets)
    {
        return new MetricsService(
            new FakeMetricsRepository(tickets),
            new FakeCurrentUserService(Guid.NewGuid(), CompanyId));
    }

    private static TicketMetricsRecord CreateTicket(
        TicketPriority priority,
        Guid assignedToId,
        string statusName,
        DateTime createdAtUtc,
        DateTime? startedAt = null,
        DateTime? resolvedAt = null,
        DateTime? responseDeadline = null)
    {
        bool isClosed = statusName.Contains("Resuelto", StringComparison.OrdinalIgnoreCase)
            || statusName.Contains("Closed", StringComparison.OrdinalIgnoreCase)
            || statusName.Contains("Cancelado", StringComparison.OrdinalIgnoreCase);

        return new TicketMetricsRecord
        {
            Id = Guid.NewGuid(),
            Priority = priority,
            StatusId = Guid.NewGuid(),
            StatusIsClosed = isClosed,
            StatusName = statusName,
            CreatedAtUtc = createdAtUtc,
            StartedWorkAtUtc = startedAt,
            ResolvedAtUtc = resolvedAt,
            ResponseDeadlineAtUtc = responseDeadline ?? createdAtUtc.AddHours(4),
            AssignedToId = assignedToId,
            AssignedToFirstName = "Test",
            AssignedToLastName = "Technician"
        };
    }
}
