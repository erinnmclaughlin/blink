using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Blink.WebApp.Configuration.Pipeline;

internal sealed class RequestValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public RequestValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators) 
    {
        _validators = validators;
    }
    
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var validationFailures = new List<ValidationFailure>();
        
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(request, ct);
            validationFailures.AddRange(result.Errors);
        }

        if (validationFailures.Count > 0)
        {
            throw new ValidationException(validationFailures);
        }

        return await next();
    }
}