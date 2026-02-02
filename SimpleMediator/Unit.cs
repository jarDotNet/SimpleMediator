namespace SimpleMediator;

// Unit type for commands that don't return a value
public struct Unit
{
    public static Unit Value => default;
}
