namespace SimpleMediator.Examples.Pipeline;

// Generic validation middleware for requests
public class ValidationMiddleware<TRequest, TResponse> : IPipelineMiddleware<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = request.GetType().Name;
        Console.WriteLine($"[Pipeline] Validating {requestName}...");

        // Example validation logic over string properties
        var properties = request.GetType().GetProperties();
        foreach (var prop in properties)
        {
            var value = prop.GetValue(request);
            if (value is string str && string.IsNullOrWhiteSpace(str))
            {
                Console.WriteLine($"[Pipeline] Warning: {prop.Name} is empty");
            }
        }

        return await next(cancellationToken).ConfigureAwait(false);
    }
}
