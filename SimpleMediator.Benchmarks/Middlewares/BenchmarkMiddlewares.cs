namespace SimpleMediator.Benchmarks.Middlewares;

// Simple pass-through middleware for pipeline benchmarks
public class PassThroughMiddleware<TRequest, TResponse> : IPipelineMiddleware<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return await next(cancellationToken);
    }
}

// Second middleware to test multiple middlewares in pipeline
public class SecondPassThroughMiddleware<TRequest, TResponse> : IPipelineMiddleware<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return await next(cancellationToken);
    }
}

// Third middleware to test pipeline with 3 middlewares
public class ThirdPassThroughMiddleware<TRequest, TResponse> : IPipelineMiddleware<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return await next(cancellationToken);
    }
}
