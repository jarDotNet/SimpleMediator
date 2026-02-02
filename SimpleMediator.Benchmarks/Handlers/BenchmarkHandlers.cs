namespace SimpleMediator.Benchmarks.Requests;

// Handler for command with response
public class SimpleCommandHandler
{
    public Task<SimpleResponse> Handle(SimpleCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SimpleResponse(1, command.Value));
    }
}

// Handler for void command (returns Unit)
public class VoidCommandHandler
{
    public Task<Unit> Handle(VoidCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Unit.Value);
    }
}

// Handler for query
public class SimpleQueryHandler
{
    public Task<SimpleResponse> Handle(SimpleQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SimpleResponse(query.Id, $"Result for {query.Id}"));
    }
}

// Multiple event handlers
public class SimpleEventHandler1
{
    public Task Handle(SimpleEvent @event, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class SimpleEventHandler2
{
    public Task Handle(SimpleEvent @event, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class SimpleEventHandler3
{
    public Task Handle(SimpleEvent @event, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
