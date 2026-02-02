using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace SimpleMediator;

public static class MediatorExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        var config = new MediatorConfiguration();
        services.AddSingleton(config);
        services.AddSingleton<Mediator>();

        var assembliesToRegister = assemblies.Length > 0 ? assemblies : [Assembly.GetCallingAssembly()];
        foreach (var assembly in assembliesToRegister)
        {
            config.AddAssembly(assembly);
            RegisterHandlersFromAssembly(services, assembly);
        }

        return services;
    }

    public static IServiceCollection AddMediatorManualHandler<T>(this IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MediatorConfiguration));
        var config = descriptor?.ImplementationInstance as MediatorConfiguration
            ?? throw new InvalidOperationException("Call AddMediator() before adding manual handlers.");

        services.TryAddTransient(typeof(T));
        config.AddAssembly(typeof(T).Assembly);

        return services;
    }

    public static IServiceCollection AddMediatorPipelineMiddleware<TRequest, TResponse, TMiddleware>(this IServiceCollection services)
        where TRequest : notnull
        where TMiddleware : class, IPipelineMiddleware<TRequest, TResponse>
    {
        services.AddTransient<IPipelineMiddleware<TRequest, TResponse>, TMiddleware>();
        return services;
    }

    public static IServiceCollection AddMediatorPipelineMiddleware(this IServiceCollection services, Type pipelineMiddlewareType)
    {
        if (!pipelineMiddlewareType.IsGenericTypeDefinition)
        {
            throw new ArgumentException(
                $"{pipelineMiddlewareType.Name} must be an open generic type.", nameof(pipelineMiddlewareType));
        }

        services.AddTransient(typeof(IPipelineMiddleware<,>), pipelineMiddlewareType);
        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Handler") && !t.IsAbstract && !t.IsInterface && t.IsClass);

        foreach (var handlerType in handlerTypes)
        {
            services.TryAddTransient(handlerType);
        }
    }
}
