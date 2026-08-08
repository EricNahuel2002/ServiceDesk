using FluentValidation;
using ServiceDesk.Application.Common.Exceptions;
using ValidationException = ServiceDesk.Application.Common.Exceptions.ValidationException;

namespace ServiceDesk.Application.Common.Validation;

public static class ValidationHelper
{
    public static async Task ValidateAsync<T>(
        IValidator<T> validator,
        T request,
        CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(request, cancellationToken);

        if (!result.IsValid)
        {
            IReadOnlyDictionary<string, string[]> errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(e => e.ErrorMessage).ToArray());

            throw new ValidationException(errors);
        }
    }
}
