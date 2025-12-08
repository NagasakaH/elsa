namespace Elsa.ServiceBus.MassTransit.Attributes;

/// <summary>
/// Marks a class as an RPC activity definition.
/// The class should inherit from RpcActivityDefinitionBase&lt;TRequest, TResponse&gt;.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class RpcActivityAttribute : Attribute
{
    /// <summary>
    /// The display name for the activity. If not specified, generated from class name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The category for the activity.
    /// </summary>
    public string Category { get; set; } = "MassTransit RPC";

    /// <summary>
    /// The description of the activity.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The destination queue name for the RPC call.
    /// </summary>
    public string? DestinationQueue { get; set; }

    /// <summary>
    /// Timeout in seconds for the RPC call.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to automatically generate inputs from request type for properties not explicitly defined.
    /// </summary>
    public bool AutoGenerateInputs { get; set; } = true;

    /// <summary>
    /// Whether to automatically generate outputs from response type for properties not explicitly defined.
    /// </summary>
    public bool AutoGenerateOutputs { get; set; } = true;

    /// <summary>
    /// Whether to flatten nested objects into individual inputs.
    /// </summary>
    public bool FlattenNested { get; set; } = false;

    /// <summary>
    /// Maximum depth for flattening nested objects.
    /// </summary>
    public int MaxFlattenDepth { get; set; } = 2;
}

/// <summary>
/// Marks a property as an explicit input for the RPC activity.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class RpcInputAttribute : Attribute
{
    /// <summary>
    /// The display name for the input.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The description of the input.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The source path in the request object (e.g., "Customer.Name").
    /// If not specified, uses the property name.
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// The order of the input in the UI.
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// The UI hint for the input (e.g., "singleline", "multiline", "checkbox", "dropdown").
    /// </summary>
    public string? UIHint { get; set; }

    /// <summary>
    /// Example value for the input.
    /// </summary>
    public string? Example { get; set; }

    /// <summary>
    /// Default value for the input.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Whether this is a required input.
    /// </summary>
    public bool IsRequired { get; set; } = false;
}

/// <summary>
/// Marks a property as a collection input that will use JSON editor.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class RpcCollectionInputAttribute : RpcInputAttribute
{
    /// <summary>
    /// Whether to use JSON input strategy (true) or Expression strategy (false).
    /// </summary>
    public bool UseJsonInput { get; set; } = true;
}

/// <summary>
/// Marks a property as an explicit output for the RPC activity.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class RpcOutputAttribute : Attribute
{
    /// <summary>
    /// The display name for the output.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The description of the output.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The source path in the response object (e.g., "Result.Value").
    /// If not specified, uses the property name.
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// The order of the output in the UI.
    /// </summary>
    public int Order { get; set; } = 0;
}

/// <summary>
/// Marks a property to be ignored when auto-generating inputs/outputs.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class RpcIgnoreAttribute : Attribute
{
}
