using SimpleMediator.Tests.MediatorTests;

namespace SimpleMediator.Tests.MediatorExtensionsTests;

/// <summary>
/// Test fixtures specific to MediatorExtensionsTests.
/// </summary>

public class ManualTestHandler1
{
    public Task<string> Handle(TestQuery query)
    {
        return Task.FromResult("Manual");
    }
}

public abstract class AbstractTestHandler
{
    public abstract Task Handle(TestQuery query);
}

public interface ITestHandler
{
    Task Handle(TestQuery query);
}

public class NotAutoRegistered
{
    public string DoSomething() => "Not a handler";
}

public class GenericValidationMiddleware<TRequest, TResponse> : IPipelineMiddleware<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return await next(cancellationToken);
    }
}
