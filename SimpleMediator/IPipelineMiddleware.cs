namespace SimpleMediator;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);

public interface IPipelineMiddleware<in TRequest, TResponse> where TRequest : notnull
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
