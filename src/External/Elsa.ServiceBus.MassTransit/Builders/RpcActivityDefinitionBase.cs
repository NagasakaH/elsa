using System.ComponentModel;
using System.Reflection;
using Elsa.ServiceBus.MassTransit.Attributes;
using Elsa.Workflows.UIHints;
using Humanizer;

namespace Elsa.ServiceBus.MassTransit.Builders;

/// <summary>
/// Base class for defining RPC activities using inheritance.
/// Inherit from this class and use [RpcActivity] attribute to define custom RPC activities.
/// </summary>
/// <typeparam name="TRequest">The request message type.</typeparam>
/// <typeparam name="TResponse">The response message type.</typeparam>
public abstract class RpcActivityDefinitionBase<TRequest, TResponse>
    where TRequest : class, new()
    where TResponse : class
{
    /// <summary>
    /// Override to customize the request builder logic.
    /// </summary>
    public virtual TRequest BuildRequest(IDictionary<string, object?> inputs)
    {
        // Default implementation - will be handled by CustomRpcActivity
        return new TRequest();
    }

    /// <summary>
    /// Override to customize the response mapping logic.
    /// </summary>
    public virtual IDictionary<string, object?> MapResponse(TResponse response)
    {
        // Default implementation - will be handled by CustomRpcActivity
        return new Dictionary<string, object?>();
    }

    /// <summary>
    /// Override to perform additional configuration on the activity definition.
    /// </summary>
    public virtual void Configure(RpcActivityBuilder<TRequest, TResponse> builder)
    {
        // Override in derived class to add custom configuration
    }
}

/// <summary>
/// Scanner that finds and registers RPC activity definitions from assemblies.
/// </summary>
public static class RpcActivityScanner
{
    /// <summary>
    /// Scans the specified assemblies for classes with [RpcActivity] attribute
    /// and creates RpcActivityDefinition instances.
    /// </summary>
    public static IEnumerable<RpcActivityDefinition> ScanAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var definition in ScanAssembly(assembly))
            {
                yield return definition;
            }
        }
    }

    /// <summary>
    /// Scans a single assembly for RPC activity definitions.
    /// </summary>
    public static IEnumerable<RpcActivityDefinition> ScanAssembly(Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<RpcActivityAttribute>() != null);

        foreach (var type in types)
        {
            var definition = CreateDefinitionFromType(type);
            if (definition != null)
            {
                yield return definition;
            }
        }
    }

    /// <summary>
    /// Creates an RpcActivityDefinition from a type with [RpcActivity] attribute.
    /// </summary>
    public static RpcActivityDefinition? CreateDefinitionFromType(Type definitionType)
    {
        var attribute = definitionType.GetCustomAttribute<RpcActivityAttribute>();
        if (attribute == null)
            return null;

        // Find the base type to get TRequest and TResponse
        var baseType = FindRpcActivityBaseType(definitionType);
        if (baseType == null)
        {
            // If not inheriting from base, look for IRpcActivityDefinition<,> interface
            var interfaceType = definitionType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && 
                    i.GetGenericTypeDefinition() == typeof(IRpcActivityDefinition<,>));
            
            if (interfaceType == null)
            {
                throw new InvalidOperationException(
                    $"Type {definitionType.Name} has [RpcActivity] attribute but does not inherit from " +
                    $"RpcActivityDefinitionBase<TRequest, TResponse> or implement IRpcActivityDefinition<TRequest, TResponse>.");
            }
            
            baseType = interfaceType;
        }

        var genericArgs = baseType.GetGenericArguments();
        var requestType = genericArgs[0];
        var responseType = genericArgs[1];

        var definition = new RpcActivityDefinition
        {
            RequestType = requestType,
            ResponseType = responseType,
            ActivityTypeName = $"Custom.{definitionType.Name}",
            DisplayName = attribute.DisplayName ?? definitionType.Name.Humanize(LetterCasing.Title),
            Category = attribute.Category,
            Description = attribute.Description,
            DestinationAddress = !string.IsNullOrEmpty(attribute.DestinationQueue) 
                ? new Uri($"queue:{attribute.DestinationQueue}") 
                : null,
            Timeout = TimeSpan.FromSeconds(attribute.TimeoutSeconds),
            AutoGenerateRemainingInputs = attribute.AutoGenerateInputs,
            AutoGenerateRemainingOutputs = attribute.AutoGenerateOutputs,
            FlattenNested = attribute.FlattenNested,
            MaxFlattenDepth = attribute.MaxFlattenDepth
        };

        // Scan for explicit input definitions
        var inputProperties = definitionType.GetProperties()
            .Where(p => p.GetCustomAttribute<RpcInputAttribute>() != null ||
                       p.GetCustomAttribute<RpcCollectionInputAttribute>() != null)
            .OrderBy(p => p.GetCustomAttribute<RpcInputAttribute>()?.Order ?? 
                         p.GetCustomAttribute<RpcCollectionInputAttribute>()?.Order ?? 0);

        foreach (var property in inputProperties)
        {
            var inputDef = CreateInputDefinition(property);
            if (inputDef != null)
            {
                definition.Inputs.Add(inputDef);
            }
        }

        // Scan for explicit output definitions
        var outputProperties = definitionType.GetProperties()
            .Where(p => p.GetCustomAttribute<RpcOutputAttribute>() != null)
            .OrderBy(p => p.GetCustomAttribute<RpcOutputAttribute>()?.Order ?? 0);

        foreach (var property in outputProperties)
        {
            var outputDef = CreateOutputDefinition(property);
            if (outputDef != null)
            {
                definition.Outputs.Add(outputDef);
            }
        }

        // Create instance and call Configure if available
        var instance = Activator.CreateInstance(definitionType);
        if (instance != null)
        {
            // Store custom builder/mapper if defined
            var buildRequestMethod = definitionType.GetMethod("BuildRequest");
            var mapResponseMethod = definitionType.GetMethod("MapResponse");

            if (buildRequestMethod != null && buildRequestMethod.DeclaringType != typeof(RpcActivityDefinitionBase<,>).MakeGenericType(requestType, responseType))
            {
                definition.RequestBuilder = inputs =>
                {
                    var result = buildRequestMethod.Invoke(instance, new object[] { inputs });
                    return result!;
                };
            }

            if (mapResponseMethod != null && mapResponseMethod.DeclaringType != typeof(RpcActivityDefinitionBase<,>).MakeGenericType(requestType, responseType))
            {
                definition.ResponseMapper = response =>
                {
                    var result = mapResponseMethod.Invoke(instance, new object[] { response });
                    return (IDictionary<string, object?>)result!;
                };
            }

            // Call Configure method using reflection with the correct builder type
            var configureMethod = definitionType.GetMethod("Configure");
            if (configureMethod != null)
            {
                var builderType = typeof(RpcActivityBuilder<,>).MakeGenericType(requestType, responseType);
                var builder = Activator.CreateInstance(builderType);
                
                // Set initial values from attribute
                var withTypeNameMethod = builderType.GetMethod("WithTypeName");
                withTypeNameMethod?.Invoke(builder, new object[] { definition.ActivityTypeName! });

                configureMethod.Invoke(instance, new object[] { builder! });

                // Merge configured values into definition
                var buildMethod = builderType.GetMethod("Build");
                var configuredDef = (RpcActivityDefinition)buildMethod!.Invoke(builder, null)!;
                
                // Merge inputs that were added via Configure
                foreach (var input in configuredDef.Inputs)
                {
                    if (!definition.Inputs.Any(i => i.Name == input.Name))
                    {
                        definition.Inputs.Add(input);
                    }
                }

                // Merge outputs that were added via Configure
                foreach (var output in configuredDef.Outputs)
                {
                    if (!definition.Outputs.Any(o => o.Name == output.Name))
                    {
                        definition.Outputs.Add(output);
                    }
                }
            }
        }

        return definition;
    }

    private static Type? FindRpcActivityBaseType(Type type)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (current.IsGenericType && 
                current.GetGenericTypeDefinition() == typeof(RpcActivityDefinitionBase<,>))
            {
                return current;
            }
            current = current.BaseType;
        }
        return null;
    }

    private static ActivityInputDefinition? CreateInputDefinition(PropertyInfo property)
    {
        var collectionAttr = property.GetCustomAttribute<RpcCollectionInputAttribute>();
        var inputAttr = collectionAttr ?? property.GetCustomAttribute<RpcInputAttribute>();
        
        if (inputAttr == null)
            return null;

        var displayName = inputAttr.DisplayName 
            ?? property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName 
            ?? property.Name.Humanize(LetterCasing.Title);

        var description = inputAttr.Description
            ?? property.GetCustomAttribute<DescriptionAttribute>()?.Description;

        var sourcePath = inputAttr.SourcePath ?? property.Name;
        var inputName = sourcePath.Replace(".", "_");

        var inputDef = new ActivityInputDefinition
        {
            Name = inputName,
            DisplayName = displayName,
            Description = description,
            Type = property.PropertyType,
            SourcePath = sourcePath,
            Order = inputAttr.Order,
            Example = inputAttr.Example,
            DefaultValue = inputAttr.DefaultValue,
            IsRequired = inputAttr.IsRequired
        };

        // Set UI hint
        if (!string.IsNullOrEmpty(inputAttr.UIHint))
        {
            inputDef.UIHint = inputAttr.UIHint;
        }
        else
        {
            inputDef.UIHint = GetDefaultUIHint(property.PropertyType);
        }

        // Handle collection input
        if (collectionAttr != null)
        {
            inputDef.CollectionStrategy = collectionAttr.UseJsonInput 
                ? CollectionInputStrategy.JsonInput 
                : CollectionInputStrategy.Expression;
            inputDef.UIHint = InputUIHints.MultiLine;
            
            // Generate example JSON for collection
            var elementType = GetCollectionElementType(property.PropertyType);
            if (elementType != null)
            {
                inputDef.Example = GenerateJsonExample(elementType);
            }
        }

        return inputDef;
    }

    private static ActivityOutputDefinition? CreateOutputDefinition(PropertyInfo property)
    {
        var outputAttr = property.GetCustomAttribute<RpcOutputAttribute>();
        if (outputAttr == null)
            return null;

        var displayName = outputAttr.DisplayName
            ?? property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
            ?? property.Name.Humanize(LetterCasing.Title);

        var description = outputAttr.Description
            ?? property.GetCustomAttribute<DescriptionAttribute>()?.Description;

        var sourcePath = outputAttr.SourcePath ?? property.Name;
        var outputName = "Response_" + sourcePath.Replace(".", "_");

        return new ActivityOutputDefinition
        {
            Name = outputName,
            DisplayName = $"Response: {displayName}",
            Description = description,
            Type = property.PropertyType,
            SourcePath = sourcePath,
            Order = outputAttr.Order
        };
    }

    private static string GetDefaultUIHint(Type type)
    {
        if (type == typeof(bool)) return InputUIHints.Checkbox;
        if (type.IsEnum) return InputUIHints.DropDown;
        if (IsCollectionType(type)) return InputUIHints.MultiLine;
        return InputUIHints.SingleLine;
    }

    private static bool IsCollectionType(Type type)
    {
        if (type == typeof(string)) return false;
        if (type.IsArray) return true;
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            return genericDef == typeof(List<>) ||
                   genericDef == typeof(IList<>) ||
                   genericDef == typeof(ICollection<>) ||
                   genericDef == typeof(IEnumerable<>);
        }
        return false;
    }

    private static Type? GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType();

        if (collectionType.IsGenericType)
            return collectionType.GetGenericArguments()[0];

        return null;
    }

    private static string GenerateJsonExample(Type elementType)
    {
        var properties = elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Take(5);

        var exampleObj = new Dictionary<string, object?>();
        foreach (var prop in properties)
        {
            exampleObj[prop.Name] = GetDefaultValueForType(prop.PropertyType);
        }

        return $"[\n  {System.Text.Json.JsonSerializer.Serialize(exampleObj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}\n]";
    }

    private static object? GetDefaultValueForType(Type type)
    {
        if (type == typeof(string)) return "sample";
        if (type == typeof(int) || type == typeof(long)) return 0;
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return 0.0;
        if (type == typeof(bool)) return false;
        if (type == typeof(DateTime)) return DateTime.Now.ToString("yyyy-MM-dd");
        if (type == typeof(Guid)) return Guid.Empty.ToString();
        return null;
    }
}

/// <summary>
/// Interface for defining RPC activities without inheriting from base class.
/// </summary>
public interface IRpcActivityDefinition<TRequest, TResponse>
    where TRequest : class, new()
    where TResponse : class
{
    TRequest BuildRequest(IDictionary<string, object?> inputs);
    IDictionary<string, object?> MapResponse(TResponse response);
}
