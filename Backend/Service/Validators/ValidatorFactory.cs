using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Service.Interfaces;

namespace Service.Validators;

public class ValidatorFactory(IServiceProvider provider) : IAppValidatorFactory
{
    private readonly IServiceProvider _provider = provider;

    public IValidator<T> Get<T>()
    {
        return _provider.GetRequiredService<IValidator<T>>();
    }
}