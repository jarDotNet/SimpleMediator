namespace SimpleMediator.Examples.Commands;

// Command - just a plain class, no interface needed
public record CreateUserCommand(string Name = "", string Email = "");

// Command handler - convention: {CommandName}Handler with Handle method
public class CreateUserCommandHandler
{
    public async Task<int> Handle(CreateUserCommand command)
    {
        // Simulate creating a user
        Console.WriteLine($"Creating user: {command.Name} ({command.Email})");
        await Task.Delay(100); // Simulate async work
        return new Random().Next(1, 1000); // Return user ID
    }
}
