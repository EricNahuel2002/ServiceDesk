using FluentValidation.Results;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Application.Features.Tickets.Validators;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.UnitTests;

public class UpdateTicketRequestValidatorTests
{
    private readonly UpdateTicketRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ReturnsNoErrors()
    {
        UpdateTicketRequest request = BuildValidRequest();

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAssignedToId_ReturnsError()
    {
        UpdateTicketRequest request = BuildValidRequest() with { AssignedToId = Guid.Empty };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTicketRequest.AssignedToId));
    }

    [Fact]
    public void Validate_InvalidPriority_ReturnsError()
    {
        UpdateTicketRequest request = BuildValidRequest() with { Priority = (TicketPriority)99 };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTicketRequest.Priority));
    }

    [Fact]
    public void Validate_EmptyStatusId_ReturnsError()
    {
        UpdateTicketRequest request = BuildValidRequest() with { StatusId = Guid.Empty };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTicketRequest.StatusId));
    }

    private static UpdateTicketRequest BuildValidRequest() =>
        new()
        {
            AssignedToId = Guid.NewGuid(),
            Priority = TicketPriority.Media,
            StatusId = Guid.NewGuid()
        };
}
