using FluentValidation;
using ServiceDesk.Application.DTOs.Auth;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Application.Features.Auth.Validators;

public sealed class AdminCreateUserRequestValidator : AbstractValidator<AdminCreateUserRequest>
{
    public AdminCreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .Length(8, 100);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.CompanyId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => Roles.All.Contains(role))
            .WithMessage("El rol no es válido.");
    }
}
