# SimpleMediator

A lightweight MediatR-like library for .NET that uses **convention-based discovery** instead of marker interfaces.

## Features

✨ **No Marker Interfaces** - Commands, Queries, and Events are just plain classes or records  
🔍 **Convention-Based** - Handlers are discovered by naming convention (e.g., `CreateUserCommand` → `CreateUserCommandHandler`)  
🔄 **Pipeline Middlewares** - Add cross-cutting concerns like logging, validation, caching  
📢 **Event Broadcasting** - Publish events to multiple handlers  
⚡ **Simple & Fast** - Compiled delegates, minimal overhead  
🎯 **CancellationToken Support** - Full async cancellation support  

## Quick Start

### 1. Register the Mediator

```csharp
using Microsoft.Extensions.DependencyInjection;
using SimpleMediator;

var services = new ServiceCollection();

// Register mediator and auto-discover handlers from assemblies
services.AddMediator(typeof(Program).Assembly);

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<Mediator>();
```

### 2. Create Commands, Queries, and Handlers

Just define plain classes—no interfaces required!

```csharp
// Command with return value
public record CreateUserCommand(string Name, string Email);

// Handler follows naming convention: {RequestName}Handler
public class CreateUserCommandHandler
{
    public async Task<int> Handle(CreateUserCommand command)
    {
        Console.WriteLine($"Creating user: {command.Name}");
        await Task.Delay(100);
        return 42; // Return user ID
    }
}
```

### 3. Send Commands/Queries

```csharp
var command = new CreateUserCommand("Alice", "alice@example.com");
var userId = await mediator.Send<int>(command);
```

---

## Usage Examples

### Commands with Return Value

```csharp
// Define the command
public record CreateUserCommand(string Name, string Email);

// Define the handler
public class CreateUserCommandHandler
{
    public async Task<int> Handle(CreateUserCommand command)
    {
        // Create user logic...
        return userId;
    }
}

// Execute
var userId = await mediator.Send<int>(new CreateUserCommand("Alice", "alice@example.com"));
```

### Commands without Return Value

```csharp
// Define the command
public record SendEmailCommand(string To, string Subject, string Body);

// Define the handler (returns Task, not Task<T>)
public class SendEmailCommandHandler
{
    public async Task Handle(SendEmailCommand command)
    {
        Console.WriteLine($"Sending email to: {command.To}");
        await Task.Delay(50);
    }
}

// Execute (no generic parameter needed)
await mediator.Send(new SendEmailCommand("bob@example.com", "Hello", "Welcome!"));
```

### Queries

Queries work exactly like commands—the naming is just for semantics:

```csharp
// Define the query
public record GetUserByIdQuery(int UserId);

// Define a DTO
public record UserDto(int Id, string Name, string Email);

// Define the handler
public class GetUserByIdQueryHandler
{
    public async Task<UserDto?> Handle(GetUserByIdQuery query)
    {
        Console.WriteLine($"Fetching user with ID: {query.UserId}");
        await Task.Delay(50);
        
        return new UserDto(query.UserId, "John Doe", "john@example.com");
    }
}

// Execute
var user = await mediator.Send<UserDto?>(new GetUserByIdQuery(42));
```

### Events (Multiple Handlers)

Events can have multiple handlers that all execute when the event is published:

```csharp
// Define the event
public record UserCreatedEvent(int UserId, string Name, string Email);

// Define multiple handlers (notice they don't end with "Handler" by default)
public class UserCreatedEventHandler1
{
    public async Task Handle(UserCreatedEvent @event)
    {
        Console.WriteLine($"[Handler 1] Logging user creation: {@event.Name}");
        await Task.Delay(50);
    }
}

public class UserCreatedEventHandler2
{
    public async Task Handle(UserCreatedEvent @event)
    {
        Console.WriteLine($"[Handler 2] Sending welcome email to: {@event.Email}");
        await Task.Delay(50);
    }
}

// Register event handlers manually (they don't follow "Handler" suffix convention)
services.AddMediatorManualHandler<UserCreatedEventHandler1>();
services.AddMediatorManualHandler<UserCreatedEventHandler2>();

// Publish the event (all handlers execute)
await mediator.Publish(new UserCreatedEvent(42, "Alice", "alice@example.com"));
```

### CancellationToken Support

Handlers can optionally accept a `CancellationToken`:

```csharp
public class CreateUserCommandHandler
{
    public async Task<int> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        return 42;
    }
}

// Pass cancellation token when sending
var cts = new CancellationTokenSource();
var userId = await mediator.Send<int>(command, cts.Token);
```

---

## Pipeline Middlewares

Pipeline middlewares allow you to add cross-cutting concerns that execute before/after your handlers. The pattern is similar to ASP.NET Core middleware.

### Creating a Pipeline Middleware

Implement `IPipelineMiddleware<TRequest, TResponse>`:

```csharp
public class LoggingMiddleware<TRequest, TResponse> : IPipelineMiddleware<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var requestName = request.GetType().Name;
        Console.WriteLine($"[Pipeline] Executing {requestName}...");

        var stopwatch = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        stopwatch.Stop();

        Console.WriteLine($"[Pipeline] {requestName} completed in {stopwatch.ElapsedMilliseconds}ms");
        return response;
    }
}
```

### Validation Middleware Example

```csharp
public class ValidationMiddleware<TRequest, TResponse> : IPipelineMiddleware<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Pipeline] Validating {request.GetType().Name}...");

        // Add your validation logic here
        var properties = request.GetType().GetProperties();
        foreach (var prop in properties)
        {
            var value = prop.GetValue(request);
            if (value is string str && string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentException($"{prop.Name} cannot be empty");
            }
        }

        return await next(cancellationToken);
    }
}
```

### Registering Pipeline Middlewares

```csharp
// Register open generic middlewares (applies to all requests)
services.AddMediatorPipelineMiddleware(typeof(LoggingMiddleware<,>));
services.AddMediatorPipelineMiddleware(typeof(ValidationMiddleware<,>));

// Or register for specific request/response types
services.AddMediatorPipelineMiddleware<CreateUserCommand, int, LoggingMiddleware<CreateUserCommand, int>>();
```

> [!IMPORTANT] 
> Middlewares execute in the order they are registered. The first registered middleware wraps the second, and so on.

---

## Registration API

### AddMediator

Registers the mediator and auto-discovers handlers from specified assemblies:

```csharp
// Single assembly
services.AddMediator(typeof(Program).Assembly);

// Multiple assemblies
services.AddMediator(
    typeof(Program).Assembly,
    typeof(MyHandlers).Assembly
);

// No assemblies = uses calling assembly
services.AddMediator();
```

### AddMediatorManualHandler

Registers handlers that don't follow the naming convention:

```csharp
services.AddMediatorManualHandler<MyCustomEventHandler1>();
```

### AddMediatorPipelineMiddleware

Registers pipeline middlewares:

```csharp
// Open generic (applies to all requests)
services.AddMediatorPipelineMiddleware(typeof(LoggingMiddleware<,>));

// Specific types
services.AddMediatorPipelineMiddleware<TRequest, TResponse, TMiddleware>();
```

---

## Handler Naming Convention

SimpleMediator discovers handlers using naming conventions:

| Request Type | Expected Handler Name |
|--------------|----------------------|
| `CreateUserCommand` | `CreateUserCommandHandler` |
| `GetUserByIdQuery` | `GetUserByIdQueryHandler` |
| `UserCreatedEvent` | `UserCreatedEventHandler` |

The handler must:
1. Be named `{RequestFullName}Handler` (including namespace)
2. Have a public `Handle` method that accepts the request type
3. Optionally accept a `CancellationToken` as the second parameter

```csharp
// Valid handler signatures
public Task<int> Handle(CreateUserCommand command)
public Task<int> Handle(CreateUserCommand command, CancellationToken ct)
public Task Handle(SendEmailCommand command)
public int Handle(SyncCommand command)  // Synchronous is also supported
```

---

## API Reference

### Mediator

| Method | Description |
|--------|-------------|
| `Task<TResponse> Send<TResponse>(object request, CancellationToken ct = default)` | Sends a request and returns a response |
| `Task Send(object command, CancellationToken ct = default)` | Sends a command without a return value |
| `Task Publish(object @event, CancellationToken ct = default)` | Publishes an event to all registered handlers |
| `Task Publish<TEvent>(TEvent @event, CancellationToken ct = default)` | Strongly-typed event publishing |

### IPipelineMiddleware<TRequest, TResponse>

```csharp
public interface IPipelineMiddleware<in TRequest, TResponse> where TRequest : notnull
{
    Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken);
}
```

---

## Why SimpleMediator?

### Comparison with MediatR

| Feature | MediatR | SimpleMediator |
|---------|---------|----------------|
| Marker Interfaces | ✅ Required (`IRequest<T>`) | ❌ Not needed |
| Handler Discovery | Interface-based | Convention-based |
| Learning Curve | Moderate | Minimal |
| Pipeline Middlewares | ✅ Yes | ✅ Yes |
| Events/Notifications | ✅ Yes | ✅ Yes |
| Dependencies | MediatR package | Only MS DI |

### When to Use SimpleMediator

- You want a **simpler, convention-based** approach
- You prefer **plain classes** without marker interfaces
- You're building a **small to medium** sized application
- You want **minimal dependencies**
- You want **easy-to-understand** source code

---

## Performance

SimpleMediator uses **compiled expression delegates** instead of reflection-based `MethodInfo.Invoke()` for handler invocation. This provides excellent performance while keeping the code simple and maintainable.

### Benchmark Results

```
| Method                        | Mean      | Allocated |
|------------------------------ |----------:|----------:|
| Send(command) - void          |  72.67 ns |     256 B |
| Send<TResponse>(command)      |  92.81 ns |     576 B |
| Publish(event) - 3 handlers   | 101.59 ns |     288 B |
| Send<TResponse>(query)        | 125.14 ns |     624 B |
```

> **Environment:** .NET 9.0, 13th Gen Intel Core i7-13800H

### How It Works

- **Handler discovery** is done once per request type and cached
- **Expression trees** are compiled to delegates at discovery time
- **ConcurrentDictionary** provides thread-safe caching
- **Minimal allocations** in the hot path

Run benchmarks yourself:

```bash
dotnet run -c Release --project SimpleMediator.Benchmarks -- --filter "*MediatorBenchmarks*"
```

---

## Requirements

- .NET 8.0 or .NET 9.0
- Microsoft.Extensions.DependencyInjection

---

## License

MIT License - feel free to use in any project!