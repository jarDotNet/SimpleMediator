namespace SimpleMediator.Examples.Events;

// Event - just a plain class
public record UserCreatedEvent(
    int UserId,
    string Name = "",
    string Email = "",
    DateTime CreatedAt = default
);

// Multiple handlers can handle the same event
public class UserCreatedEventHandler1
{
    public async Task Handle(UserCreatedEvent @event)
    {
        Console.WriteLine($"[Handler 1] User created: {@event.Name} (ID: {@event.UserId})");
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

public class UserCreatedEventHandler3
{
    public async Task Handle(UserCreatedEvent @event)
    {
        Console.WriteLine($"[Handler 3] Logging user creation event at {@event.CreatedAt}");
        await Task.Delay(50);
    }
}
