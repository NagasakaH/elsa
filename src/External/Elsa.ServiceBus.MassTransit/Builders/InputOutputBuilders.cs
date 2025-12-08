using System.Text.Json;
using Elsa.Workflows.UIHints;

namespace Elsa.ServiceBus.MassTransit.Builders;

/// <summary>
/// Builder for input property definitions.
/// </summary>
public class InputBuilder
{
    private readonly ActivityInputDefinition _definition;

    public InputBuilder(string name, string displayName, Type type)
    {
        _definition = new ActivityInputDefinition
        {
            Name = name,
            DisplayName = displayName,
            Type = type
        };
        
        // Set default UI hints based on type
        SetDefaultUIHint(type);
    }

    private void SetDefaultUIHint(Type type)
    {
        if (type == typeof(bool))
            _definition.UIHint = InputUIHints.Checkbox;
        else if (type.IsEnum)
            _definition.UIHint = InputUIHints.DropDown;
        else if (type == typeof(string))
            _definition.UIHint = InputUIHints.SingleLine;
        else if (IsNumericType(type))
            _definition.UIHint = InputUIHints.SingleLine;
        else
            _definition.UIHint = InputUIHints.MultiLine;
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(short) ||
               type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) ||
               type == typeof(float) || type == typeof(double) || type == typeof(decimal) ||
               type == typeof(byte) || type == typeof(sbyte);
    }

    public InputBuilder WithDescription(string description)
    {
        _definition.Description = description;
        return this;
    }

    public InputBuilder WithDefaultValue(object? defaultValue)
    {
        _definition.DefaultValue = defaultValue;
        return this;
    }

    public InputBuilder IsOptional()
    {
        _definition.IsRequired = false;
        return this;
    }

    public InputBuilder IsRequired(bool required = true)
    {
        _definition.IsRequired = required;
        return this;
    }

    public InputBuilder WithSourcePath(string path)
    {
        _definition.SourcePath = path;
        return this;
    }

    public InputBuilder WithUIHint(string uiHint)
    {
        _definition.UIHint = uiHint;
        return this;
    }

    public InputBuilder WithOptions(IDictionary<string, object> options)
    {
        _definition.Options = options;
        return this;
    }

    public InputBuilder WithExample(string example)
    {
        _definition.Example = example;
        return this;
    }

    public InputBuilder WithOrder(int order)
    {
        _definition.Order = order;
        return this;
    }

    /// <summary>
    /// Configures this input as a JSON collection input.
    /// </summary>
    public InputBuilder AsJsonCollection(Type? elementType = null)
    {
        _definition.CollectionStrategy = CollectionInputStrategy.JsonInput;
        _definition.ElementType = elementType;
        _definition.UIHint = InputUIHints.MultiLine;
        _definition.Type = typeof(string); // JSON is stored as string
        return this;
    }

    /// <summary>
    /// Configures this input as an expression-based collection input.
    /// </summary>
    public InputBuilder AsExpressionCollection(Type? elementType = null)
    {
        _definition.CollectionStrategy = CollectionInputStrategy.Expression;
        _definition.ElementType = elementType;
        _definition.UIHint = InputUIHints.MultiLine;
        return this;
    }

    public ActivityInputDefinition Build() => _definition;
}

/// <summary>
/// Builder for collection input property definitions.
/// </summary>
public class CollectionInputBuilder<TItem>
{
    private readonly ActivityInputDefinition _definition;
    private readonly Type _itemType = typeof(TItem);

    public CollectionInputBuilder(string name, string displayName, string sourcePath)
    {
        _definition = new ActivityInputDefinition
        {
            Name = name,
            DisplayName = displayName,
            Type = typeof(string), // JSON is stored as string
            SourcePath = sourcePath,
            CollectionStrategy = CollectionInputStrategy.JsonInput,
            ElementType = _itemType,
            UIHint = InputUIHints.MultiLine
        };

        // Generate default description with JSON example
        GenerateDefaultDescription();
    }

    private void GenerateDefaultDescription()
    {
        var example = GenerateJsonExample();
        _definition.Description = $"JSON配列形式で入力してください。\n例:\n{example}";
        _definition.Example = example;
    }

    private string GenerateJsonExample()
    {
        try
        {
            var exampleItem = CreateExampleItem();
            var exampleArray = new[] { exampleItem };
            return JsonSerializer.Serialize(exampleArray, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch
        {
            return "[]";
        }
    }

    private object CreateExampleItem()
    {
        var instance = Activator.CreateInstance(_itemType);
        if (instance == null) return new { };

        // Set example values for primitive properties
        foreach (var prop in _itemType.GetProperties())
        {
            if (!prop.CanWrite) continue;

            object? exampleValue = prop.PropertyType switch
            {
                Type t when t == typeof(string) => "例",
                Type t when t == typeof(int) => 1,
                Type t when t == typeof(long) => 1L,
                Type t when t == typeof(double) => 1.0,
                Type t when t == typeof(decimal) => 1.0m,
                Type t when t == typeof(bool) => true,
                Type t when t == typeof(DateTime) => DateTime.Now,
                Type t when t.IsEnum => Enum.GetValues(t).GetValue(0),
                _ => null
            };

            if (exampleValue != null)
            {
                try { prop.SetValue(instance, exampleValue); } catch { }
            }
        }

        return instance;
    }

    public CollectionInputBuilder<TItem> WithDescription(string description)
    {
        _definition.Description = description;
        return this;
    }

    public CollectionInputBuilder<TItem> WithDefaultValue(string jsonDefaultValue)
    {
        _definition.DefaultValue = jsonDefaultValue;
        return this;
    }

    public CollectionInputBuilder<TItem> WithExample(string jsonExample)
    {
        _definition.Example = jsonExample;
        _definition.Description = $"JSON配列形式で入力してください。\n例:\n{jsonExample}";
        return this;
    }

    public CollectionInputBuilder<TItem> IsOptional()
    {
        _definition.IsRequired = false;
        return this;
    }

    public CollectionInputBuilder<TItem> WithOrder(int order)
    {
        _definition.Order = order;
        return this;
    }

    /// <summary>
    /// Use expression mode instead of JSON input.
    /// </summary>
    public CollectionInputBuilder<TItem> AsExpression()
    {
        _definition.CollectionStrategy = CollectionInputStrategy.Expression;
        _definition.Description = "JavaScript式またはLiquid式でコレクションを返してください。\n例: getVariable('items')";
        return this;
    }

    public ActivityInputDefinition Build() => _definition;
}

/// <summary>
/// Builder for output property definitions.
/// </summary>
public class OutputBuilder
{
    private readonly ActivityOutputDefinition _definition;

    public OutputBuilder(string name, string displayName, Type type)
    {
        _definition = new ActivityOutputDefinition
        {
            Name = name,
            DisplayName = displayName,
            Type = type
        };
    }

    public OutputBuilder WithDescription(string description)
    {
        _definition.Description = description;
        return this;
    }

    public OutputBuilder WithSourcePath(string path)
    {
        _definition.SourcePath = path;
        return this;
    }

    public OutputBuilder WithOrder(int order)
    {
        _definition.Order = order;
        return this;
    }

    public ActivityOutputDefinition Build() => _definition;
}
