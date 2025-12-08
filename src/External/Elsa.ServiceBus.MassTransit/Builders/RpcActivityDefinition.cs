namespace Elsa.ServiceBus.MassTransit.Builders;

/// <summary>
/// Strategy for handling collection/list inputs in activities.
/// </summary>
public enum CollectionInputStrategy
{
    /// <summary>
    /// コレクション全体をJSON文字列として入力。
    /// UI: MultiLine テキストエリア（JSONエディタ）
    /// </summary>
    JsonInput,

    /// <summary>
    /// JavaScript/Liquid式でコレクションを構築。
    /// UI: CodeEditor（式エディタ）
    /// </summary>
    Expression
}

/// <summary>
/// Defines an RPC activity with customizable input/output mappings.
/// </summary>
public class RpcActivityDefinition
{
    /// <summary>
    /// The request message type.
    /// </summary>
    public required Type RequestType { get; set; }

    /// <summary>
    /// The response message type.
    /// </summary>
    public required Type ResponseType { get; set; }

    /// <summary>
    /// Custom activity type name. If not specified, generated from request type.
    /// </summary>
    public string? ActivityTypeName { get; set; }

    /// <summary>
    /// The display name for the activity.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The category for the activity.
    /// </summary>
    public string Category { get; set; } = "MassTransit RPC";

    /// <summary>
    /// Description of the activity.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The destination queue address.
    /// </summary>
    public Uri? DestinationAddress { get; set; }

    /// <summary>
    /// The timeout for the RPC operation.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Input property definitions.
    /// </summary>
    public List<ActivityInputDefinition> Inputs { get; } = new();

    /// <summary>
    /// Output property definitions.
    /// </summary>
    public List<ActivityOutputDefinition> Outputs { get; } = new();

    /// <summary>
    /// Whether to automatically generate inputs from request type for properties not explicitly defined.
    /// </summary>
    public bool AutoGenerateRemainingInputs { get; set; } = true;

    /// <summary>
    /// Whether to automatically generate outputs from response type for properties not explicitly defined.
    /// </summary>
    public bool AutoGenerateRemainingOutputs { get; set; } = true;

    /// <summary>
    /// Whether to flatten nested objects in auto-generation.
    /// </summary>
    public bool FlattenNested { get; set; } = false;

    /// <summary>
    /// Maximum depth for flattening nested objects.
    /// </summary>
    public int MaxFlattenDepth { get; set; } = 2;

    /// <summary>
    /// Custom request builder function.
    /// </summary>
    public Func<IDictionary<string, object?>, object>? RequestBuilder { get; set; }

    /// <summary>
    /// Custom response mapper function.
    /// </summary>
    public Func<object, IDictionary<string, object?>>? ResponseMapper { get; set; }
}

/// <summary>
/// Defines an input property for an activity.
/// </summary>
public class ActivityInputDefinition
{
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public required Type Type { get; set; }
    public object? DefaultValue { get; set; }
    public bool IsRequired { get; set; } = true;
    public string? UIHint { get; set; }
    public IDictionary<string, object>? Options { get; set; }
    
    /// <summary>
    /// Path to the property in the original nested object (e.g., "Address.City").
    /// </summary>
    public string? SourcePath { get; set; }
    
    /// <summary>
    /// For collection types, specifies how to handle the input.
    /// </summary>
    public CollectionInputStrategy? CollectionStrategy { get; set; }

    /// <summary>
    /// For collection types, the element type.
    /// </summary>
    public Type? ElementType { get; set; }

    /// <summary>
    /// Example value for documentation (shown in description).
    /// </summary>
    public string? Example { get; set; }

    /// <summary>
    /// Order of the input in the UI.
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// Defines an output property for an activity.
/// </summary>
public class ActivityOutputDefinition
{
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public required Type Type { get; set; }
    
    /// <summary>
    /// Path to the property in the original nested response object.
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// Order of the output in the UI.
    /// </summary>
    public int Order { get; set; }
}
