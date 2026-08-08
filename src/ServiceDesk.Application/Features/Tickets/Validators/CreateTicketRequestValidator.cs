using FluentValidation;
using ServiceDesk.Application.DTOs.Tickets;

namespace ServiceDesk.Application.Features.Tickets.Validators;

public sealed class CreateTicketRequestValidator : AbstractValidator<CreateTicketRequest>
{
    private const int MaxFilesPerTicket = 10;

    private const long MaxFileSizeInBytes = 50L * 1024 * 1024;

    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "video/mp4",
        "video/webm"
    ];

    public CreateTicketRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty();

        RuleFor(x => x.CategoryId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Files)
            .Must(files => files.Count <= MaxFilesPerTicket)
            .WithMessage($"No se pueden adjuntar más de {MaxFilesPerTicket} archivos por ticket.");

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
