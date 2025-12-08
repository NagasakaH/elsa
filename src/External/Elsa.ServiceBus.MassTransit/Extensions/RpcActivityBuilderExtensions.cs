using System.Reflection;
using Elsa.ServiceBus.MassTransit.Builders;
using Elsa.ServiceBus.MassTransit.Features;

namespace Elsa.ServiceBus.MassTransit.Extensions;

/// <summary>
/// Extension methods for adding custom RPC activities using the fluent builder.
/// </summary>
public static class RpcActivityBuilderExtensions
{
    private const string CustomRpcDefinitionsKey = "CustomRpcActivityDefinitions";

    /// <summary>
    /// Adds a custom RPC activity definition using the fluent builder.
    /// This allows customizing inputs/outputs, flattening nested objects, and handling collections with JSON editor.
    /// </summary>
    public static MassTransitFeature AddRpcActivity<TRequest, TResponse>(
        this MassTransitFeature feature,
        Action<RpcActivityBuilder<TRequest, TResponse>> configure)
        where TRequest : class, new()
        where TResponse : class
    {
        var builder = new RpcActivityBuilder<TRequest, TResponse>();
        configure(builder);
        var definition = builder.Build();
        
        feature.AddCustomRpcActivityDefinition(definition);
        return feature;
    }

    /// <summary>
    /// Adds an RPC activity definition class directly.
    /// The class must have [RpcActivity] attribute and inherit from RpcActivityDefinitionBase.
    /// </summary>
    public static MassTransitFeature AddRpcActivity<TDefinition>(this MassTransitFeature feature)
        where TDefinition : class
    {
        var definition = RpcActivityScanner.CreateDefinitionFromType(typeof(TDefinition));
        if (definition != null)
        {
            feature.AddCustomRpcActivityDefinition(definition);
        }
        return feature;
    }

    /// <summary>
    /// Scans the specified assembly for classes with [RpcActivity] attribute
    /// and adds them as activity definitions.
    /// </summary>
    public static MassTransitFeature AddRpcActivitiesFromAssembly(
        this MassTransitFeature feature, 
        Assembly assembly)
    {
        var definitions = RpcActivityScanner.ScanAssembly(assembly);
        foreach (var definition in definitions)
        {
            feature.AddCustomRpcActivityDefinition(definition);
        }
        return feature;
    }

    /// <summary>
    /// Scans the assembly containing the specified type for classes with [RpcActivity] attribute
    /// and adds them as activity definitions.
    /// </summary>
    public static MassTransitFeature AddRpcActivitiesFromAssemblyContaining<T>(
        this MassTransitFeature feature)
    {
        return feature.AddRpcActivitiesFromAssembly(typeof(T).Assembly);
    }

    /// <summary>
    /// Scans the assembly containing the specified type for classes with [RpcActivity] attribute
    /// and adds them as activity definitions.
    /// </summary>
    public static MassTransitFeature AddRpcActivitiesFromAssemblyContaining(
        this MassTransitFeature feature,
        Type type)
    {
        return feature.AddRpcActivitiesFromAssembly(type.Assembly);
    }

    /// <summary>
    /// Scans multiple assemblies for classes with [RpcActivity] attribute
    /// and adds them as activity definitions.
    /// </summary>
    public static MassTransitFeature AddRpcActivitiesFromAssemblies(
        this MassTransitFeature feature,
        params Assembly[] assemblies)
    {
        var definitions = RpcActivityScanner.ScanAssemblies(assemblies);
        foreach (var definition in definitions)
        {
            feature.AddCustomRpcActivityDefinition(definition);
        }
        return feature;
    }

    /// <summary>
    /// Adds a custom RPC activity definition directly.
    /// </summary>
    internal static MassTransitFeature AddCustomRpcActivityDefinition(
        this MassTransitFeature feature,
        RpcActivityDefinition definition)
    {
        if (!feature.Module.Properties.TryGetValue(CustomRpcDefinitionsKey, out var value))
        {
            value = new List<RpcActivityDefinition>();
            feature.Module.Properties[CustomRpcDefinitionsKey] = value;
        }
        
        var definitions = (List<RpcActivityDefinition>)value;
        definitions.Add(definition);
        return feature;
    }

    /// <summary>
    /// Gets all custom RPC activity definitions.
    /// </summary>
    internal static IEnumerable<RpcActivityDefinition> GetCustomRpcActivityDefinitions(
        this MassTransitFeature feature)
    {
        if (feature.Module.Properties.TryGetValue(CustomRpcDefinitionsKey, out var value))
        {
            return (List<RpcActivityDefinition>)value;
        }
        return Enumerable.Empty<RpcActivityDefinition>();
    }
}
