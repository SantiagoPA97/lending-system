using FluentValidation;

namespace Lending.Api.Common;

public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is not null)
        {
            var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
            if (validator is not null)
            {
                var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
                if (!result.IsValid)
                    throw new ValidationException(result.Errors);
            }
        }

        return await next(context);
    }
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) where T : class =>
        builder.AddEndpointFilter<ValidationFilter<T>>();
}
