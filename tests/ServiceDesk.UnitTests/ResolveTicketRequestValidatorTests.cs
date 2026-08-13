using FluentValidation.Results;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Application.Features.Tickets.Validators;

namespace ServiceDesk.UnitTests;

public class ResolveTicketRequestValidatorTests
{
    private readonly ResolveTicketRequestValidator _validator = new();

    [Fact]
    public void Validate_NullResolutionNote_ReturnsNoErrors()
    {
        ResolveTicketRequest request = new();

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyResolutionNote_ReturnsNoErrors()
    {
        ResolveTicketRequest request = new() { ResolutionNote = string.Empty };

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NoteWithinLimit_ReturnsNoErrors()
    {
        ResolveTicketRequest request = new() { ResolutionNote = new string('a', 2000) };

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NoteExceedingLimit_ReturnsError()
    {
        ResolveTicketRequest request = new() { ResolutionNote = new string('a', 2001) };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ResolveTicketRequest.ResolutionNote));
    }
}
