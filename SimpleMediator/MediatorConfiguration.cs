using System.Reflection;

namespace SimpleMediator;

public class MediatorConfiguration
{
    // Using HashSet to prevent duplicate assembly scanning
    public HashSet<Assembly> HandlerAssemblies { get; } = [];

    public void AddAssembly(Assembly assembly)
    {
        HandlerAssemblies.Add(assembly);
    }
}