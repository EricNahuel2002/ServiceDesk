using FluentValidation;
using ServiceDesk.Application.DTOs.Auth;

namespace ServiceDesk.Application.Features.Auth.Validators;

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}
