using Domain.Exceptions.Custom;
using FluentValidation;

namespace Service.Utils;

public static class ValidationHelper
{
    public static async Task ValidateAndThrowAsync<T>(IValidator<T> validator, T instance)
    {
        var result = await validator.ValidateAsync(instance);
        if (!result.IsValid)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
            throw new EntityValidationException(errors);
        }
    }
}
