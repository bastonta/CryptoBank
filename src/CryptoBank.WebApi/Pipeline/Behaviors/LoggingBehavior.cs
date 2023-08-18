using Mediator;

namespace CryptoBank.WebApi.Pipeline.Behaviors;

public class LoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse> where TMessage : IMessage
{
    private readonly ILogger _logger;

    public LoggingBehavior(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("LoggingBehavior");
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        CancellationToken cancellationToken,
        MessageHandlerDelegate<TMessage, TResponse> next)
    {
        _logger.LogInformation("Handling {RequestType}", message.GetType().FullName);
        _logger.LogDebug("Request: {@Request}", message);

        var response = await next(message, cancellationToken);

        _logger.LogInformation("Handled {RequestType}", message.GetType().FullName);
        _logger.LogDebug("Response: {@Response}", response);

        return response;
    }
}
