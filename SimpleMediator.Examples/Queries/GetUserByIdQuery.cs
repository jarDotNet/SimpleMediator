namespace SimpleMediator.Examples.Queries;

// Query - just a plain record
public record GetUserByIdQuery(int UserId);

// Query handler - convention: {QueryName}Handler
public class GetUserByIdQueryHandler
{
    public async Task<UserDto?> Handle(GetUserByIdQuery query)
    {
        Console.WriteLine($"Fetching user with ID: {query.UserId}");
        await Task.Delay(50);

        // Simulate database lookup
        if (query.UserId <= 0) return null;

        return new UserDto(
            Id: query.UserId,
            Name: "John Doe",
            Email: "john@example.com"
        );
    }
}
