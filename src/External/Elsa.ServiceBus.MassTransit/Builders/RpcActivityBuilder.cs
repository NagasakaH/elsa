using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Elsa.Workflows.UIHints;
using Humanizer;

namespace Elsa.ServiceBus.MassTransit.Builders;

/// <summary>
/// Fluent builder for creating RPC activity definitions.
/// </summary>
public class RpcActivityBuilder<TRequest, TResponse>
    where TRequest : class, new()
    where TResponse : class
{
    private readonly RpcActivityDefinition _definition;
    private int _inputOrder = 0;
    private int _outputOrder = 0;

    public RpcActivityBuilder()
    {
        _definition = new RpcActivityDefinition
        {
            RequestType = typeof(TRequest),
            ResponseType = typeof(TResponse)
        };
    }

    /// <summary>
    /// Sets a custom activity type name.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithTypeName(string typeName)
    {
        _definition.ActivityTypeName = typeName;
        return this;
    }

    /// <summary>
    /// Sets the display name for the activity.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithDisplayName(string displayName)
    {
        _definition.DisplayName = displayName;
        return this;
    }

    /// <summary>
    /// Sets the category for the activity.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithCategory(string category)
    {
        _definition.Category = category;
        return this;
    }

    /// <summary>
    /// Sets the description for the activity.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithDescription(string description)
    {
        _definition.Description = description;
        return this;
    }

    /// <summary>
    /// Sets the destination queue name.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithDestination(string queueName)
    {
        _definition.DestinationAddress = new Uri($"queue:{queueName}");
        return this;
    }

    /// <summary>
    /// Sets the destination queue URI.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithDestinationUri(Uri destinationAddress)
    {
        _definition.DestinationAddress = destinationAddress;
        return this;
    }

    /// <summary>
    /// Sets the timeout for the RPC operation.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithTimeout(TimeSpan timeout)
    {
        _definition.Timeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the timeout for the RPC operation in seconds.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithTimeoutSeconds(int seconds)
    {
        _definition.Timeout = TimeSpan.FromSeconds(seconds);
        return this;
    }

    /// <summary>
    /// Configures automatic input/output generation for remaining properties.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithAutoGeneration(
        bool inputs = true, 
        bool outputs = true, 
        bool flattenNested = false, 
        int maxDepth = 2)
    {
        _definition.AutoGenerateRemainingInputs = inputs;
        _definition.AutoGenerateRemainingOutputs = outputs;
        _definition.FlattenNested = flattenNested;
        _definition.MaxFlattenDepth = maxDepth;
        return this;
    }

    /// <summary>
    /// Disables automatic input/output generation - only explicitly defined properties will be used.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithManualMappingOnly()
    {
        _definition.AutoGenerateRemainingInputs = false;
        _definition.AutoGenerateRemainingOutputs = false;
        return this;
    }

    /// <summary>
    /// Adds a simple input property.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithInput<TValue>(
        string name,
        string displayName,
        Action<InputBuilder>? configure = null)
    {
        var builder = new InputBuilder(name, displayName, typeof(TValue));
        builder.WithOrder(_inputOrder++);
        configure?.Invoke(builder);
        _definition.Inputs.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds an input mapped from a request property using expression.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithInput<TValue>(
        Expression<Func<TRequest, TValue>> propertySelector,
        string? displayName = null,
        Action<InputBuilder>? configure = null)
    {
        return WithInput(propertySelector, displayName, null, configure);
    }

    /// <summary>
    /// Adds an input mapped from a request property using expression with description.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithInput<TValue>(
        Expression<Func<TRequest, TValue>> propertySelector,
        string? displayName,
        string? description,
        Action<InputBuilder>? configure = null)
    {
        var path = GetPropertyPath(propertySelector);
        var propertyName = path.Replace(".", "_");
        var property = GetPropertyFromPath(typeof(TRequest), path);
        
        var resolvedDisplayName = displayName 
            ?? property?.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName 
            ?? property?.Name.Humanize(LetterCasing.Title) 
            ?? propertyName;

        var propertyType = typeof(TValue);
        var builder = new InputBuilder(propertyName, resolvedDisplayName, propertyType);
        builder.WithSourcePath(path);
        builder.WithOrder(_inputOrder++);
        
        // Use provided description or fall back to attribute
        if (!string.IsNullOrEmpty(description))
        {
            builder.WithDescription(description);
        }
        else if (property != null)
        {
            var descAttr = property.GetCustomAttribute<DescriptionAttribute>();
            if (descAttr != null)
                builder.WithDescription(descAttr.Description);
        }

        // Handle collection types automatically
        if (IsCollectionType(propertyType))
        {
            var elementType = GetCollectionElementType(propertyType);
            builder.AsJsonCollection(elementType);
        }
        
        configure?.Invoke(builder);
        _definition.Inputs.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a collection input with JSON editor.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithCollectionInput<TItem>(
        Expression<Func<TRequest, IEnumerable<TItem>>> propertySelector,
        string? displayName = null,
        Action<CollectionInputBuilder<TItem>>? configure = null)
    {
        return WithCollectionInput(propertySelector, displayName, null, configure);
    }

    /// <summary>
    /// Adds a collection input with JSON editor and description.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithCollectionInput<TItem>(
        Expression<Func<TRequest, IEnumerable<TItem>>> propertySelector,
        string? displayName,
        string? description,
        Action<CollectionInputBuilder<TItem>>? configure = null)
    {
        var path = GetPropertyPath(propertySelector);
        var propertyName = path.Replace(".", "_");
        var property = GetPropertyFromPath(typeof(TRequest), path);

        var resolvedDisplayName = displayName
            ?? property?.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
            ?? property?.Name.Humanize(LetterCasing.Title)
            ?? propertyName;

        var builder = new CollectionInputBuilder<TItem>(propertyName, resolvedDisplayName, path);
        builder.WithOrder(_inputOrder++);
        
        if (!string.IsNullOrEmpty(description))
        {
            builder.WithDescription(description);
        }
        else if (property != null)
        {
            var descAttr = property.GetCustomAttribute<DescriptionAttribute>();
            if (descAttr != null)
                builder.WithDescription(descAttr.Description);
        }

        configure?.Invoke(builder);
        _definition.Inputs.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a Dictionary/Map input with JSON editor.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithDictionaryInput<TKey, TValue>(
        Expression<Func<TRequest, IDictionary<TKey, TValue>>> propertySelector,
        string? displayName = null,
        Action<InputBuilder>? configure = null)
    {
        var path = GetPropertyPath(propertySelector);
        var propertyName = path.Replace(".", "_");
        var property = GetPropertyFromPath(typeof(TRequest), path);

        var resolvedDisplayName = displayName
            ?? property?.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
            ?? property?.Name.Humanize(LetterCasing.Title)
            ?? propertyName;

        var builder = new InputBuilder(propertyName, resolvedDisplayName, typeof(IDictionary<TKey, TValue>));
        builder.WithSourcePath(path);
        builder.WithOrder(_inputOrder++);
        builder.WithUIHint(InputUIHints.MultiLine);
        builder.WithDescription($"JSON形式で入力: {{\"key\": \"value\"}}");
        
        if (property != null)
        {
            var descAttr = property.GetCustomAttribute<DescriptionAttribute>();
            if (descAttr != null)
                builder.WithDescription(descAttr.Description + "\nJSON形式で入力: {\"key\": \"value\"}");
        }

        configure?.Invoke(builder);
        _definition.Inputs.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds an output property.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithOutput<TValue>(
        string name,
        string displayName,
        Action<OutputBuilder>? configure = null)
    {
        var builder = new OutputBuilder(name, displayName, typeof(TValue));
        builder.WithOrder(_outputOrder++);
        configure?.Invoke(builder);
        _definition.Outputs.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds an output mapped from a response property using expression.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithOutput<TValue>(
        Expression<Func<TResponse, TValue>> propertySelector,
        string? displayName = null,
        Action<OutputBuilder>? configure = null)
    {
        return WithOutput(propertySelector, displayName, null, configure);
    }

    /// <summary>
    /// Adds an output mapped from a response property using expression with description.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithOutput<TValue>(
        Expression<Func<TResponse, TValue>> propertySelector,
        string? displayName,
        string? description,
        Action<OutputBuilder>? configure = null)
    {
        var path = GetPropertyPath(propertySelector);
        var propertyName = "Response_" + path.Replace(".", "_");
        var property = GetPropertyFromPath(typeof(TResponse), path);

        var resolvedDisplayName = displayName
            ?? $"Response: {property?.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? property?.Name.Humanize(LetterCasing.Title) ?? path}";

        var builder = new OutputBuilder(propertyName, resolvedDisplayName, typeof(TValue));
        builder.WithSourcePath(path);
        builder.WithOrder(_outputOrder++);

        if (!string.IsNullOrEmpty(description))
        {
            builder.WithDescription(description);
        }
        else if (property != null)
        {
            var descAttr = property.GetCustomAttribute<DescriptionAttribute>();
            if (descAttr != null)
                builder.WithDescription(descAttr.Description);
        }

        configure?.Invoke(builder);
        _definition.Outputs.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Sets a custom request builder function.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithRequestBuilder(
        Func<IDictionary<string, object?>, TRequest> builder)
    {
        _definition.RequestBuilder = inputs => builder(inputs);
        return this;
    }

    /// <summary>
    /// Sets a custom response mapper function.
    /// </summary>
    public RpcActivityBuilder<TRequest, TResponse> WithResponseMapper(
        Func<TResponse, IDictionary<string, object?>> mapper)
    {
        _definition.ResponseMapper = response => mapper((TResponse)response);
        return this;
    }

    /// <summary>
    /// Builds the activity definition.
    /// </summary>
    public RpcActivityDefinition Build()
    {
        return _definition;
    }

    #region Helper Methods

    private static string GetPropertyPath<T, TValue>(Expression<Func<T, TValue>> propertySelector)
    {
        var parts = new List<string>();
        Expression current = propertySelector.Body;

        // Handle Convert expressions (for nullable types, etc.)
        if (current is UnaryExpression unaryExpr && unaryExpr.NodeType == ExpressionType.Convert)
        {
            current = unaryExpr.Operand;
        }

        while (current is MemberExpression memberExpr)
        {
            parts.Insert(0, memberExpr.Member.Name);
            current = memberExpr.Expression!;
        }

        return string.Join(".", parts);
    }

    private static PropertyInfo? GetPropertyFromPath(Type type, string path)
    {
        var parts = path.Split('.');
        PropertyInfo? property = null;
        var currentType = type;

        foreach (var part in parts)
        {
            property = currentType.GetProperty(part, BindingFlags.Public | BindingFlags.Instance);
            if (property == null) return null;
            currentType = property.PropertyType;
        }

        return property;
    }

    private static bool IsCollectionType(Type type)
    {
        if (type == typeof(string)) return false;
        if (type.IsArray) return true;
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>) || 
                genericDef == typeof(IList<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IEnumerable<>))
            {
                return true;
            }
        }
        return false;
    }

    private static Type? GetCollectionElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();
        
        if (type.IsGenericType)
            return type.GetGenericArguments().FirstOrDefault();
        
        return null;
    }

    #endregion
}
