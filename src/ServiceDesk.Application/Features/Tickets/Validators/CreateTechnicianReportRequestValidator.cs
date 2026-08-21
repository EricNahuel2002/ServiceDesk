using FluentValidation;
using ServiceDesk.Application.DTOs.Tickets;

namespace ServiceDesk.Application.Features.Tickets.Validators;

public sealed class CreateTechnicianReportRequestValidator : AbstractValidator<CreateTechnicianReportRequest>
{
    private const int MaxFilesPerReport = 10;

    private const long MaxFileSizeInBytes = 50L * 1024 * 1024;

    private const int MaxReasonLength = 2000;

    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "video/mp4",
        "video/webm"
    ];

    public CreateTechnicianReportRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(MaxReasonLength)
            .WithMessage($"El motivo no puede superar los {MaxReasonLength} caracteres.");

        RuleFor(x => x.Files)
            .Must(files => files.Count <= MaxFilesPerReport)
            .WithMessage($"No se pueden adjuntar más de {MaxFilesPerReport} archivos por reporte.");

        RuleForEach(x => x.Files)
            .ChildRules(file =>
            {
                file.RuleFor(f => f.FileName)
                    .NotEmpty()
                    .MaximumLength(255);

                file.RuleFor(f => f.ContentType)
                    .Must(AllowedContentTypes.Contains)
                    .WithMessage("El tipo de archivo no está permitido.");

                file.RuleFor(f => f.SizeInBytes)
                    .GreaterThan(0)
                    .LessThanOrEqualTo(MaxFileSizeInBytes);
            });
    }
}
