using FluentValidation;
using ServiceDesk.Application.DTOs.Catalog;

namespace ServiceDesk.Application.Features.Catalog.Validators;

public sealed class CreateStatusRequestValidator : AbstractValidator<CreateStatusRequest>
{
    public CreateStatusRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}
