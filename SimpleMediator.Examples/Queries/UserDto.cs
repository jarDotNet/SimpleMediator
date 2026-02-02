namespace SimpleMediator.Examples.Queries;

public record UserDto(
    int Id,
    string Name = "",
    string Email = ""
);