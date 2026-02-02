namespace SimpleMediator.Examples.Commands;

// Command without return value
public record SendEmailCommand(string To = "", string Subject = "", string Body = "");

// Command handler
public class SendEmailCommandHandler
{
    public async Task Handle(SendEmailCommand command)
    {
        Console.WriteLine($"Sending email to: {command.To}");
        Console.WriteLine($"Subject: {command.Subject}");
        await Task.Delay(50);
    }
}
