using FluentValidation;
using Mediator;

namespace CryptoBank.WebApi.Pipeline.Behaviors;

public class ValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse> where TMessage : IMessage
{
    private readonly IValidator<TMessage>? _validator;

    public ValidationBehavior(IValidator<TMessage>? validator = null)
    {
        _validator = validator;
    }

    public async ValueTask<TResponse> Handle(TMessage message, CancellationToken cancellationToken, MessageHandlerDelegate<TMessage, TResponse> next)
    {
        if (_validator is not null)
        {
            await _validator.ValidateAndThrowAsync(message, cancellationToken);
        }

        return await next(message, cancellationToken);
    }
}
