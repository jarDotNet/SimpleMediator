using Microsoft.Extensions.DependencyInjection;
using SimpleMediator;
using SimpleMediator.Examples.Commands;
using SimpleMediator.Examples.Events;
using SimpleMediator.Examples.Pipeline;
using SimpleMediator.Examples.Queries;

Console.WriteLine("=== Simple Mediator Demo ===\n");

// Setup dependency injection
var services = new ServiceCollection();

// Register mediator and auto-discover handlers
services.AddMediator(typeof(Program).Assembly);

// Register pipeline middlewares
services.AddMediatorPipelineMiddleware(typeof(LoggingMiddleware<,>));
services.AddMediatorPipelineMiddleware(typeof(ValidationMiddleware<,>));

// Register event handlers manually (since they do not follow the “Ends with Handler” convention)
services.AddMediatorManualHandler<UserCreatedEventHandler1>();
services.AddMediatorManualHandler<UserCreatedEventHandler2>();
services.AddMediatorManualHandler<UserCreatedEventHandler3>();

var serviceProvider = services.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<Mediator>();

Console.WriteLine("--- Command: Create User ---");
var createUserCommand = new CreateUserCommand
{
    Name = "Alice Johnson",
    Email = "alice@example.com"
};
var userId = await mediator.Send<int>(createUserCommand);
Console.WriteLine($"Result: User created with ID {userId}\n");

Console.WriteLine("--- Command: Send Email (no return) ---");
var sendEmailCommand = new SendEmailCommand
{
    To = "bob@example.com",
    Subject = "Welcome!",
    Body = "Welcome to our platform"
};
await mediator.Send(sendEmailCommand);
Console.WriteLine();

Console.WriteLine("--- Query: Get User By ID ---");
var getUserQuery = new GetUserByIdQuery(UserId: 42);
var user = await mediator.Send<UserDto?>(getUserQuery);
if (user is not null)
{
    Console.WriteLine($"Result: {user.Name} ({user.Email})\n");
}

Console.WriteLine("--- Query: Get All Users ---");
var getAllUsersQuery = new GetAllUsersQuery { PageSize = 3, PageNumber = 1 };
var users = await mediator.Send<System.Collections.Generic.List<UserDto>>(getAllUsersQuery);
Console.WriteLine($"Result: Retrieved {users.Count} users\n");

Console.WriteLine("--- Event: User Created (multiple handlers) ---");
var userCreatedEvent = new UserCreatedEvent(
    UserId: userId,
    Name: "Alice Johnson",
    Email: "alice@example.com"
);
await mediator.Publish(userCreatedEvent);
Console.WriteLine();


Console.WriteLine("=== Demo Complete ===");
