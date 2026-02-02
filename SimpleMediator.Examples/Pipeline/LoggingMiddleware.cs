using System.Diagnostics;

namespace SimpleMediator.Examples.Pipeline;

// Generic logging middleware for requests
public class LoggingMiddleware<TRequest, TResponse> : IPipelineMiddleware<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = request.GetType().Name;
        Console.WriteLine($"[Pipeline] Executing {requestName}...");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            Console.WriteLine($"[Pipeline] {requestName} completed in {stopwatch.ElapsedMilliseconds}ms");

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"[Pipeline] {requestName} failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
            throw;
        }
    }
}
