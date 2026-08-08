using FluentValidation.Results;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Application.Features.Tickets.Validators;

namespace ServiceDesk.UnitTests;

public class CreateTicketRequestValidatorTests
{
    private readonly CreateTicketRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ReturnsNoErrors()
    {
        CreateTicketRequest request = BuildValidRequest();

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyTitle_ReturnsError()
    {
        CreateTicketRequest request = BuildValidRequest() with { Title = string.Empty };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTicketRequest.Title));
    }

    [Fact]
    public void Validate_EmptyDescription_ReturnsError()
    {
        CreateTicketRequest request = BuildValidRequest() with { Description = string.Empty };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTicketRequest.Description));
    }

    [Fact]
    public void Validate_EmptyCategoryId_ReturnsError()
    {
        CreateTicketRequest request = BuildValidRequest() with { CategoryId = Guid.Empty };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTicketRequest.CategoryId));
    }

    [Fact]
    public void Validate_MoreThanTenFiles_ReturnsError()
    {
        List<TicketFileUpload> files = Enumerable.Range(0, 11)
            .Select(_ => BuildValidFile())
            .ToList();

        CreateTicketRequest request = BuildValidRequest() with { Files = files };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTicketRequest.Files));
    }

    [Fact]
    public void Validate_DisallowedContentType_ReturnsError()
    {
        CreateTicketRequest request = BuildValidRequest() with
        {
            Files = new[]
            {
                BuildValidFile() with { ContentType = "application/octet-stream" }
            }
        };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.Contains("ContentType"));
    }

    [Fact]
    public void Validate_FileExceedingMaxSize_ReturnsError()
    {
        CreateTicketRequest request = BuildValidRequest() with
        {
            Files = new[]
            {
                BuildValidFile() with { SizeInBytes = 50L * 1024 * 1024 + 1 }
            }
        };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.Contains("SizeInBytes"));
    }

    [Fact]
    public void Validate_ValidImageAttachment_ReturnsNoErrors()
    {
        CreateTicketRequest request = BuildValidRequest() with
        {
            Files = new[]
            {
                BuildValidFile() with
                {
                    FileName = "foto.png",
                    ContentType = "image/png",
                    SizeInBytes = 1024
                }
            }
        };

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    private static CreateTicketRequest BuildValidRequest() =>
        new()
        {
            Title = "No enciende la PC",
            Description = "La PC no enciende desde esta mañana.",
            CategoryId = Guid.NewGuid()
        };

    private static TicketFileUpload BuildValidFile() =>
        new()
        {
            FileName = "archivo.jpg",
            ContentType = "image/jpeg",
            SizeInBytes = 2048,
            Content = [1, 2, 3]
        };
}
