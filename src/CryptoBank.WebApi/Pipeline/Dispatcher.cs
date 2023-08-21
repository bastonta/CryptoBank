using Mediator;

namespace CryptoBank.WebApi.Pipeline;

public class Dispatcher
{
    private readonly IMediator _mediator;

    public Dispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<TResponse> Dispatch<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        return await _mediator.Send(request, cancellationToken);
    }
}
