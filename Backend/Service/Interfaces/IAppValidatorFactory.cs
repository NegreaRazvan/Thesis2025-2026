namespace Service.Interfaces;

public interface IAppValidatorFactory
{
    FluentValidation.IValidator<T> Get<T>();
}
