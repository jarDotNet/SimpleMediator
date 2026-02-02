namespace SimpleMediator.Examples.Queries;

// Query example - just a plain class
public class GetAllUsersQuery
{
    public int PageSize { get; set; } = 10;
    public int PageNumber { get; set; } = 1;
}

// Query handler - convention: {QueryName}Handler
public class GetAllUsersQueryHandler
{
    public async Task<List<UserDto>> Handle(GetAllUsersQuery query)
    {
        Console.WriteLine($"Fetching users - Page {query.PageNumber}, Size {query.PageSize}");
        await Task.Delay(100);

        return Enumerable.Range(1, query.PageSize)
            .Select(i => new UserDto(
                Id: i,
                Name: $"User {i}",
                Email: $"user{i}@example.com"
            ))
            .ToList();
    }
}
