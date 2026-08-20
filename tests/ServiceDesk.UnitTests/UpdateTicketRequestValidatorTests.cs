using FluentValidation.Results;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Application.Features.Tickets.Validators;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.UnitTests;

public class UpdateTicketRequestValidatorTests
{
    private readonly UpdateTicketRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequestWithPriority_ReturnsNoErrors()
    {
        UpdateTicketRequest request = new()
        {
            Priority = TicketPriority.Media
        };

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidRequestWithAssignedToId_ReturnsNoErrors()
    {
        UpdateTicketRequest request = new()
        {
            AssignedToId = Guid.NewGuid()
        };

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidRequestWithBoth_ReturnsNoErrors()
    {
        UpdateTicketRequest request = new()
        {
            AssignedToId = Guid.NewGuid(),
            Priority = TicketPriority.Alta
        };

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullFields_ReturnsError()
    {
        UpdateTicketRequest request = new()
        {
            AssignedToId = null,
            Priority = null
        };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_InvalidPriority_ReturnsError()
    {
        UpdateTicketRequest request = new()
        {
            Priority = (TicketPriority)99
        };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTicketRequest.Priority));
    }

    [Fact]
    public void Validate_NoFieldsProvided_ReturnsError()
    {
        UpdateTicketRequest request = new();

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("al menos un campo"));
    }
}
