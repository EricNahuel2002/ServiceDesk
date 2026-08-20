using FluentValidation;
using ServiceDesk.Application.DTOs.Catalog;

namespace ServiceDesk.Application.Features.Catalog.Validators;

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
