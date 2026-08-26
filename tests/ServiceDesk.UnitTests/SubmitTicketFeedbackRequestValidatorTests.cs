using FluentValidation.Results;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Application.Features.Tickets.Validators;

namespace ServiceDesk.UnitTests;

public class SubmitTicketFeedbackRequestValidatorTests
{
    private readonly SubmitTicketFeedbackRequestValidator _validator = new();

    [Fact]
    public void Validate_OnlyWasSolved_ReturnsNoErrors()
    {
        SubmitTicketFeedbackRequest request = new() { WasSolved = true };

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RatingWithinRange_ReturnsNoErrors()
    {
        SubmitTicketFeedbackRequest request = new() { WasSolved = true, Rating = 5, Comment = "Excelente" };

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_RatingOutOfRange_ReturnsError(int rating)
    {
        SubmitTicketFeedbackRequest request = new() { WasSolved = true, Rating = rating };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SubmitTicketFeedbackRequest.Rating));
    }

    [Fact]
    public void Validate_CommentExceedingLimit_ReturnsError()
    {
        SubmitTicketFeedbackRequest request = new() { WasSolved = false, Comment = new string('a', 2001) };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SubmitTicketFeedbackRequest.Comment));
    }
}
