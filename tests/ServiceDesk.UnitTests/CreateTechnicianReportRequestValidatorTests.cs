using FluentValidation.Results;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Application.Features.Tickets.Validators;

namespace ServiceDesk.UnitTests;

public class CreateTechnicianReportRequestValidatorTests
{
    private readonly CreateTechnicianReportRequestValidator _validator = new();

    [Fact]
    public void Validate_WithoutFilesAndReason_ReturnsNoErrors()
    {
        CreateTechnicianReportRequest request = new();

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ReasonWithinLimit_ReturnsNoErrors()
    {
        CreateTechnicianReportRequest request = new() { Reason = new string('a', 2000) };

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ReasonExceedingLimit_ReturnsError()
    {
        CreateTechnicianReportRequest request = new() { Reason = new string('a', 2001) };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTechnicianReportRequest.Reason));
    }

    [Fact]
    public void Validate_ValidImageFile_ReturnsNoErrors()
    {
        CreateTechnicianReportRequest request = new()
        {
            Files =
            [
                new TicketFileUpload
                {
                    FileName = "captura.png",
                    ContentType = "image/png",
                    SizeInBytes = 1024,
                    Content = new MemoryStream([0x89, 0x50])
                }
            ]
        };

        ValidationResult result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DisallowedContentType_ReturnsError()
    {
        CreateTechnicianReportRequest request = new()
        {
            Files =
            [
                new TicketFileUpload
                {
                    FileName = "documento.pdf",
                    ContentType = "application/pdf",
                    SizeInBytes = 1024,
                    Content = new MemoryStream([0x25, 0x50])
                }
            ]
        };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_TooManyFiles_ReturnsError()
    {
        CreateTechnicianReportRequest request = new()
        {
            Files = Enumerable.Range(0, 11)
                .Select(index => new TicketFileUpload
                {
                    FileName = $"foto-{index}.png",
                    ContentType = "image/png",
                    SizeInBytes = 512,
                    Content = new MemoryStream([0x89])
                })
                .ToList()
        };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_FileExceedingMaxSize_ReturnsError()
    {
        CreateTechnicianReportRequest request = new()
        {
            Files =
            [
                new TicketFileUpload
                {
                    FileName = "video.mp4",
                    ContentType = "video/mp4",
                    SizeInBytes = 51L * 1024 * 1024,
                    Content = new MemoryStream([0x00])
                }
            ]
        };

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
