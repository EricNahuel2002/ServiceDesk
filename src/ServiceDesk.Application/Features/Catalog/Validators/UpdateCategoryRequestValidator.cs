using FluentValidation;
using ServiceDesk.Application.DTOs.Catalog;

namespace ServiceDesk.Application.Features.Catalog.Validators;

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
