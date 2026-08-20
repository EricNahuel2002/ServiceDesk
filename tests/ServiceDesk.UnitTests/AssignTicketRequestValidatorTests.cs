using FluentValidation.Results;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Application.Features.Tickets.Validators;

namespace ServiceDesk.UnitTests;

public class AssignTicketRequestValidatorTests
{
    private readonly AssignTicketRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ReturnsNoErrors()
    {
        AssignTicketRequest request = new() { AssignedToId = Guid.NewGuid() };

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAssignedToId_ReturnsError()
    {
        AssignTicketRequest request = new() { AssignedToId = Guid.Empty };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AssignTicketRequest.AssignedToId));
    }
}
