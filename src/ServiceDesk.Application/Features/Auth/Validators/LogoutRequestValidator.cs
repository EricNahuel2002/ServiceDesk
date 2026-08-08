using FluentValidation;
using ServiceDesk.Application.DTOs.Auth;

namespace ServiceDesk.Application.Features.Auth.Validators;

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}
