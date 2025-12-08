using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.ServiceBus.MassTransit.Activities;
using Elsa.ServiceBus.MassTransit.Builders;
using Elsa.ServiceBus.MassTransit.Models;
using Elsa.ServiceBus.MassTransit.Options;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.ServiceBus.MassTransit.Services;

/// <summary>
/// Provides activities to the system from the configured MassTransit message types.
/// </summary>
[UsedImplicitly]
public class MassTransitActivityTypeProvider(IActivityFactory activityFactory, IOptions<MassTransitActivityOptions> options) : IActivityProvider
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var messageTypes = options.Value.MessageTypes;
        var rpcMessageTypes = options.Value.RpcMessageTypes;
        var customRpcDefinitions = options.Value.CustomRpcActivityDefinitions;
        var descriptors = CreateDescriptors(messageTypes, rpcMessageTypes, customRpcDefinitions);
        return new ValueTask<IEnumerable<ActivityDescriptor>>(descriptors.ToList());
    }

    private IEnumerable<ActivityDescriptor> CreateDescriptors(
        IEnumerable<Type> messageTypes, 
        IEnumerable<RpcMessageTypeDefinition> rpcMessageTypes,
        IEnumerable<RpcActivityDefinition> customRpcDefinitions)
    {
        var descriptors = new List<ActivityDescriptor>();
        foreach (var messageType in messageTypes)
        {
            descriptors.Add(CreateMessageReceivedDescriptor(messageType));
            
            if(messageType.IsClass)
                descriptors.Add(CreatePublishMessageDescriptor(messageType));
        }

        // Add RPC activity descriptors from simple definitions
        foreach (var rpcType in rpcMessageTypes)
        {
            descriptors.Add(CreateSendRequestDescriptor(rpcType));
            descriptors.Add(CreateSendRequestWaitResponseDescriptor(rpcType));
        }

        // Add custom RPC activity descriptors from fluent builder definitions
        foreach (var customDef in customRpcDefinitions)
        {
            descriptors.Add(CreateCustomRpcActivityDescriptor(customDef));
        }
        
        return descriptors;
    }

    private ActivityDescriptor CreateMessageReceivedDescriptor(Type messageType)
    {
        var activityAttr = messageType.GetCustomAttribute<ActivityAttribute>();
        var typeName = activityAttr?.Type ?? messageType.Name;
        var fullTypeName = ActivityTypeNameHelper.GenerateTypeName(messageType);
        var displayNameAttr = messageType.GetCustomAttribute<DisplayNameAttribute>();
        var displayName = displayNameAttr?.DisplayName ?? activityAttr?.DisplayName ?? typeName.Humanize(LetterCasing.Title);
        var categoryAttr = messageType.GetCustomAttribute<CategoryAttribute>();
        var category = categoryAttr?.Category ?? activityAttr?.Category ?? "MassTransit";
        var descriptionAttr = messageType.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttr?.Description ?? activityAttr?.Description;
        
        // Create output descriptors for each property of the message type
        var outputDescriptors = CreateOutputDescriptorsFromType(messageType);

        return new()
        {
            Name = typeName,
            TypeName = fullTypeName,
            Version = 1,
            DisplayName = displayName,
            Description = description,
            Category = category,
            Kind = ActivityKind.Trigger,
            IsBrowsable = true,
            Outputs = outputDescriptors,
            Constructor = context =>
            {
                var activity = activityFactory.Create<MessageReceived>(context);
                activity.Type = fullTypeName;
                activity.MessageType = messageType;
                return activity;
            }
        };
    }

    private ActivityDescriptor CreatePublishMessageDescriptor(Type messageType)
    {
        var activityAttr = messageType.GetCustomAttribute<ActivityAttribute>();
        var typeName = activityAttr?.Type ?? messageType.Name;
        var ns = ActivityTypeNameHelper.GenerateNamespace(messageType);
        var fullTypeName = ns + ".Publish" + typeName;
        var displayNameAttr = messageType.GetCustomAttribute<DisplayNameAttribute>();
        var displayName = "Publish " + (displayNameAttr?.DisplayName ?? activityAttr?.DisplayName ?? typeName.Humanize(LetterCasing.Title));
        var categoryAttr = messageType.GetCustomAttribute<CategoryAttribute>();
        var category = categoryAttr?.Category ?? activityAttr?.Category ?? "MassTransit";
        var descriptionAttr = messageType.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttr?.Description ?? activityAttr?.Description;

        // Create input descriptors for each property of the message type
        var inputDescriptors = CreateInputDescriptorsFromType(messageType);

        return new()
        {
            Name = typeName,
            TypeName = fullTypeName,
            Version = 1,
            DisplayName = displayName,
            Description = description,
            Category = category,
            Kind = ActivityKind.Action,
            IsBrowsable = true,
            Inputs = inputDescriptors,
            Constructor = context =>
            {
                var activity = activityFactory.Create<PublishMessage>(context);
                activity.Type = fullTypeName;
                activity.MessageType = messageType;
                return activity;
            }
        };
    }

    /// <summary>
    /// Creates input descriptors from the properties of a message type.
    /// </summary>
    private List<InputDescriptor> CreateInputDescriptorsFromType(Type messageType)
    {
        var inputDescriptors = new List<InputDescriptor>();
        var properties = messageType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var property in properties)
        {
            var propertyDisplayName = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName 
                ?? property.Name.Humanize(LetterCasing.Title);
            var propertyDescription = property.GetCustomAttribute<DescriptionAttribute>()?.Description;
            
            var (uiHint, uiSpecifications) = GetUIHintForType(property.PropertyType);
            
            // For enum types, we use string as the underlying type to avoid serialization issues
            var inputType = property.PropertyType.IsEnum ? typeof(string) : property.PropertyType;

            // Type should be the naked type (e.g., string, not Input<string>)
            // IsWrapped=true tells Elsa that the value stored in SyntheticProperties is already wrapped in Input<T>
            var inputDescriptor = new InputDescriptor
            {
                Name = property.Name,
                DisplayName = propertyDisplayName,
                Description = propertyDescription,
                Type = inputType,
                IsWrapped = true,
                IsSynthetic = true,
                UIHint = uiHint,
                UISpecifications = uiSpecifications,
                ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(property.Name),
                ValueSetter = (activity, value) => activity.SyntheticProperties[property.Name] = value!
            };

            inputDescriptors.Add(inputDescriptor);
        }

        return inputDescriptors;
    }

    /// <summary>
    /// Creates output descriptors from the properties of a message type.
    /// </summary>
    private List<OutputDescriptor> CreateOutputDescriptorsFromType(Type messageType)
    {
        var outputDescriptors = new List<OutputDescriptor>();
        var properties = messageType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (var property in properties)
        {
            var propertyDisplayName = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName 
                ?? property.Name.Humanize(LetterCasing.Title);
            var propertyDescription = property.GetCustomAttribute<DescriptionAttribute>()?.Description;

            // For enum types, we use string as the underlying type
            var outputType = property.PropertyType.IsEnum ? typeof(string) : property.PropertyType;

            // Type should be the naked type (e.g., string, not Output<string>)
            var outputDescriptor = new OutputDescriptor
            {
                Name = property.Name,
                DisplayName = propertyDisplayName,
                Description = propertyDescription,
                Type = outputType,
                IsSynthetic = true,
                ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(property.Name),
                ValueSetter = (activity, value) => activity.SyntheticProperties[property.Name] = value!
            };

            outputDescriptors.Add(outputDescriptor);
        }

        return outputDescriptors;
    }

    /// <summary>
    /// Gets the appropriate UI hint and specifications for a given property type.
    /// </summary>
    private static (string UIHint, IDictionary<string, object>? UISpecifications) GetUIHintForType(Type propertyType)
    {
        if (propertyType == typeof(bool))
            return (InputUIHints.Checkbox, null);
        
        if (propertyType == typeof(string))
            return (InputUIHints.SingleLine, null);
        
        if (IsNumericType(propertyType))
            return (InputUIHints.SingleLine, null);
        
        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTimeOffset))
            return (InputUIHints.SingleLine, null);
        
        // For enum types, use dropdown with select list items
        if (propertyType.IsEnum)
        {
            var enumNames = Enum.GetNames(propertyType);
            var selectListItems = enumNames.Select(name => new { Text = name, Value = name }).ToArray();
            
            var dropdownProps = new Dictionary<string, object>
            {
                ["selectList"] = new { items = selectListItems }
            };
            
            var uiSpecifications = new Dictionary<string, object>
            {
                [InputUIHints.DropDown] = dropdownProps
            };
            
            return (InputUIHints.DropDown, uiSpecifications);
        }
        
        // For complex types, use multi-line (JSON)
        return (InputUIHints.MultiLine, null);
    }

    private static bool IsNumericType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType == typeof(int) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(byte) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(uint) ||
               underlyingType == typeof(ulong) ||
               underlyingType == typeof(ushort);
    }

    /// <summary>
    /// Creates a descriptor for the synchronous SendRequest activity.
    /// </summary>
    private ActivityDescriptor CreateSendRequestDescriptor(RpcMessageTypeDefinition rpcType)
    {
        var requestType = rpcType.RequestType;
        var responseType = rpcType.ResponseType;
        var timeout = rpcType.Timeout ?? TimeSpan.FromSeconds(30);

        var activityAttr = requestType.GetCustomAttribute<ActivityAttribute>();
        var typeName = activityAttr?.Type ?? requestType.Name;
        var ns = ActivityTypeNameHelper.GenerateNamespace(requestType);
        var fullTypeName = ns + ".Request" + typeName;
        var displayNameAttr = requestType.GetCustomAttribute<DisplayNameAttribute>();
        var displayName = "Request " + (displayNameAttr?.DisplayName ?? activityAttr?.DisplayName ?? typeName.Humanize(LetterCasing.Title));
        var categoryAttr = requestType.GetCustomAttribute<CategoryAttribute>();
        var category = categoryAttr?.Category ?? activityAttr?.Category ?? "MassTransit RPC";
        var descriptionAttr = requestType.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttr?.Description ?? activityAttr?.Description ?? $"Sends a {typeName} request and waits synchronously for a {responseType.Name} response.";

        // Create input descriptors from request type
        var inputDescriptors = CreateInputDescriptorsFromType(requestType);

        // Create output descriptors from response type (prefixed with Response_)
        var outputDescriptors = CreateRpcOutputDescriptorsFromType(responseType);

        return new()
        {
            Name = "Request" + typeName,
            TypeName = fullTypeName,
            Version = 1,
            DisplayName = displayName,
            Description = description,
            Category = category,
            Kind = ActivityKind.Action,
            IsBrowsable = true,
            Inputs = inputDescriptors,
            Outputs = outputDescriptors,
            Constructor = context =>
            {
                var activity = activityFactory.Create<SendRequest>(context);
                activity.Type = fullTypeName;
                activity.RequestType = requestType;
                activity.ResponseType = responseType;
                activity.Timeout = timeout;
                activity.DestinationAddress = rpcType.DestinationAddress;
                return activity;
            }
        };
    }

    /// <summary>
    /// Creates a descriptor for the asynchronous SendRequestWaitResponse activity.
    /// </summary>
    private ActivityDescriptor CreateSendRequestWaitResponseDescriptor(RpcMessageTypeDefinition rpcType)
    {
        var requestType = rpcType.RequestType;
        var responseType = rpcType.ResponseType;
        var timeout = rpcType.Timeout ?? TimeSpan.FromSeconds(30);

        var activityAttr = requestType.GetCustomAttribute<ActivityAttribute>();
        var typeName = activityAttr?.Type ?? requestType.Name;
        var ns = ActivityTypeNameHelper.GenerateNamespace(requestType);
        var fullTypeName = ns + ".RequestAsync" + typeName;
        var displayNameAttr = requestType.GetCustomAttribute<DisplayNameAttribute>();
        var displayName = "Request Async " + (displayNameAttr?.DisplayName ?? activityAttr?.DisplayName ?? typeName.Humanize(LetterCasing.Title));
        var categoryAttr = requestType.GetCustomAttribute<CategoryAttribute>();
        var category = categoryAttr?.Category ?? activityAttr?.Category ?? "MassTransit RPC";
        var descriptionAttr = requestType.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttr?.Description ?? activityAttr?.Description ?? $"Sends a {typeName} request and waits asynchronously (with bookmark) for a {responseType.Name} response.";

        // Create input descriptors from request type
        var inputDescriptors = CreateInputDescriptorsFromType(requestType);

        // Create output descriptors from response type (prefixed with Response_)
        var outputDescriptors = CreateRpcOutputDescriptorsFromType(responseType);

        return new()
        {
            Name = "RequestAsync" + typeName,
            TypeName = fullTypeName,
            Version = 1,
            DisplayName = displayName,
            Description = description,
            Category = category,
            Kind = ActivityKind.Trigger,
            IsBrowsable = true,
            Inputs = inputDescriptors,
            Outputs = outputDescriptors,
            Constructor = context =>
            {
                var activity = activityFactory.Create<SendRequestWaitResponse>(context);
                activity.Type = fullTypeName;
                activity.RequestType = requestType;
                activity.ResponseType = responseType;
                activity.Timeout = timeout;
                activity.DestinationAddress = rpcType.DestinationAddress;
                return activity;
            }
        };
    }

    /// <summary>
    /// Creates an activity descriptor from a custom RPC activity definition.
    /// </summary>
    private ActivityDescriptor CreateCustomRpcActivityDescriptor(RpcActivityDefinition definition)
    {
        var requestType = definition.RequestType;
        var responseType = definition.ResponseType;
        var timeout = definition.Timeout;

        var ns = ActivityTypeNameHelper.GenerateNamespace(requestType);
        var fullTypeName = definition.ActivityTypeName ?? $"{ns}.Custom.{requestType.Name}";
        var displayName = definition.DisplayName ?? $"Custom: {requestType.Name.Humanize(LetterCasing.Title)}";
        var category = definition.Category;
        var description = definition.Description ?? $"Sends a {requestType.Name} request and waits for a {responseType.Name} response.";

        // Create input descriptors from definition
        var inputDescriptors = CreateInputDescriptorsFromDefinition(definition);

        // Create output descriptors from definition
        var outputDescriptors = CreateOutputDescriptorsFromDefinition(definition);

        return new()
        {
            Name = $"Custom{requestType.Name}",
            TypeName = fullTypeName,
            Version = 1,
            DisplayName = displayName,
            Description = description,
            Category = category,
            Kind = ActivityKind.Task,
            IsBrowsable = true,
            Inputs = inputDescriptors,
            Outputs = outputDescriptors,
            CustomProperties = new Dictionary<string, object>
            {
                ["Outcomes"] = new[] { "Done", "Timeout", "Error" }
            },
            Constructor = context =>
            {
                // Don't use activityFactory.Create to avoid JSON key lookup issues
                // Instead, create the activity directly and let Elsa handle synthetic inputs
                var activity = new CustomRpcActivity
                {
                    Id = context.ActivityDescriptor.Name + "_" + Guid.NewGuid().ToString("N")[..8],
                    Type = fullTypeName,
                    RequestType = requestType,
                    ResponseType = responseType,
                    Timeout = timeout,
                    DestinationAddress = definition.DestinationAddress,
                    InputDefinitions = definition.Inputs,
                    OutputDefinitions = definition.Outputs,
                    RequestBuilder = definition.RequestBuilder,
                    ResponseMapper = definition.ResponseMapper
                };
                return activity;
            }
        };
    }

    /// <summary>
    /// Creates input descriptors from a custom RPC activity definition.
    /// </summary>
    private List<InputDescriptor> CreateInputDescriptorsFromDefinition(RpcActivityDefinition definition)
    {
        var inputDescriptors = new List<InputDescriptor>();
        var explicitInputNames = definition.Inputs.Select(i => i.SourcePath ?? i.Name).ToHashSet();

        // Add explicitly defined inputs
        foreach (var input in definition.Inputs.OrderBy(i => i.Order))
        {
            var inputType = input.Type;
            
            // For collections stored as JSON, use string type
            if (input.CollectionStrategy == CollectionInputStrategy.JsonInput)
            {
                inputType = typeof(string);
            }

            var inputDescriptor = new InputDescriptor
            {
                Name = input.Name,
                DisplayName = input.DisplayName,
                Description = BuildInputDescription(input),
                Type = inputType,
                IsWrapped = true,
                IsSynthetic = true,
                UIHint = input.UIHint ?? InputUIHints.SingleLine,
                DefaultValue = input.DefaultValue,
                ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(input.Name),
                ValueSetter = (activity, value) => activity.SyntheticProperties[input.Name] = value!
            };

            inputDescriptors.Add(inputDescriptor);
        }

        // Auto-generate remaining inputs if enabled
        if (definition.AutoGenerateRemainingInputs)
        {
            var autoInputs = CreateAutoInputsFromType(
                definition.RequestType, 
                explicitInputNames, 
                definition.FlattenNested, 
                definition.MaxFlattenDepth);
            inputDescriptors.AddRange(autoInputs);
        }

        return inputDescriptors;
    }

    private string BuildInputDescription(ActivityInputDefinition input)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(input.Description))
            parts.Add(input.Description);
        
        if (!string.IsNullOrEmpty(input.Example))
            parts.Add($"例: {input.Example}");

        return string.Join("\n", parts);
    }

    private List<InputDescriptor> CreateAutoInputsFromType(
        Type type, 
        HashSet<string> excludePaths, 
        bool flatten, 
        int maxDepth, 
        string prefix = "", 
        int currentDepth = 0)
    {
        var inputDescriptors = new List<InputDescriptor>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var property in properties)
        {
            var sourcePath = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
            
            // Skip if explicitly defined
            if (excludePaths.Contains(sourcePath) || excludePaths.Contains(property.Name))
                continue;

            var isComplexType = IsComplexType(property.PropertyType);
            var isCollectionType = IsCollectionType(property.PropertyType);

            if (flatten && isComplexType && !isCollectionType && currentDepth < maxDepth)
            {
                // Recursively flatten nested objects
                var nestedInputs = CreateAutoInputsFromType(
                    property.PropertyType,
                    excludePaths,
                    flatten,
                    maxDepth,
                    sourcePath,
                    currentDepth + 1
                );
                inputDescriptors.AddRange(nestedInputs);
            }
            else
            {
                var inputName = sourcePath.Replace(".", "_");
                var displayName = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                    ?? property.Name.Humanize(LetterCasing.Title);

                if (!string.IsNullOrEmpty(prefix))
                {
                    displayName = $"{prefix.Replace(".", " > ")} > {displayName}";
                }

                var inputType = property.PropertyType;
                var uiHint = GetSimpleUIHintForType(property.PropertyType);

                // Handle collection types as JSON
                if (isCollectionType)
                {
                    inputType = typeof(string);
                    uiHint = InputUIHints.MultiLine;
                }

                var inputDescriptor = new InputDescriptor
                {
                    Name = inputName,
                    DisplayName = displayName,
                    Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description,
                    Type = inputType.IsEnum ? typeof(string) : inputType,
                    IsWrapped = true,
                    IsSynthetic = true,
                    UIHint = uiHint,
                    ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(inputName),
                    ValueSetter = (activity, value) => activity.SyntheticProperties[inputName] = value!
                };

                inputDescriptors.Add(inputDescriptor);
            }
        }

        return inputDescriptors;
    }

    /// <summary>
    /// Creates output descriptors from a custom RPC activity definition.
    /// </summary>
    private List<OutputDescriptor> CreateOutputDescriptorsFromDefinition(RpcActivityDefinition definition)
    {
        var outputDescriptors = new List<OutputDescriptor>();
        var explicitOutputNames = definition.Outputs.Select(o => o.SourcePath ?? o.Name).ToHashSet();

        // Add explicitly defined outputs
        foreach (var output in definition.Outputs.OrderBy(o => o.Order))
        {
            var outputDescriptor = new OutputDescriptor
            {
                Name = output.Name,
                DisplayName = output.DisplayName,
                Description = output.Description,
                Type = output.Type,
                IsSynthetic = true,
                ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(output.Name),
                ValueSetter = (activity, value) => activity.SyntheticProperties[output.Name] = value!
            };

            outputDescriptors.Add(outputDescriptor);
        }

        // Auto-generate remaining outputs if enabled
        if (definition.AutoGenerateRemainingOutputs)
        {
            var autoOutputs = CreateAutoOutputsFromType(
                definition.ResponseType,
                explicitOutputNames,
                definition.FlattenNested,
                definition.MaxFlattenDepth);
            outputDescriptors.AddRange(autoOutputs);
        }

        // Add error-related outputs
        outputDescriptors.Add(new OutputDescriptor
        {
            Name = "ErrorType",
            DisplayName = "エラータイプ",
            Description = "エラーが発生した場合のタイプ（Timeout または Error）",
            Type = typeof(string),
            IsSynthetic = true,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault("ErrorType"),
            ValueSetter = (activity, value) => activity.SyntheticProperties["ErrorType"] = value!
        });

        outputDescriptors.Add(new OutputDescriptor
        {
            Name = "ErrorMessage",
            DisplayName = "エラーメッセージ",
            Description = "エラーが発生した場合のメッセージ",
            Type = typeof(string),
            IsSynthetic = true,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault("ErrorMessage"),
            ValueSetter = (activity, value) => activity.SyntheticProperties["ErrorMessage"] = value!
        });

        outputDescriptors.Add(new OutputDescriptor
        {
            Name = "ErrorDetails",
            DisplayName = "エラー詳細",
            Description = "エラーの詳細情報（スタックトレースなど）",
            Type = typeof(string),
            IsSynthetic = true,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault("ErrorDetails"),
            ValueSetter = (activity, value) => activity.SyntheticProperties["ErrorDetails"] = value!
        });

        return outputDescriptors;
    }

    private List<OutputDescriptor> CreateAutoOutputsFromType(
        Type type,
        HashSet<string> excludePaths,
        bool flatten,
        int maxDepth,
        string prefix = "",
        int currentDepth = 0)
    {
        var outputDescriptors = new List<OutputDescriptor>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (var property in properties)
        {
            var sourcePath = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

            // Skip if explicitly defined
            if (excludePaths.Contains(sourcePath) || excludePaths.Contains($"Response_{sourcePath.Replace(".", "_")}"))
                continue;

            var isComplexType = IsComplexType(property.PropertyType);
            var isCollectionType = IsCollectionType(property.PropertyType);

            if (flatten && isComplexType && !isCollectionType && currentDepth < maxDepth)
            {
                var nestedOutputs = CreateAutoOutputsFromType(
                    property.PropertyType,
                    excludePaths,
                    flatten,
                    maxDepth,
                    sourcePath,
                    currentDepth + 1
                );
                outputDescriptors.AddRange(nestedOutputs);
            }
            else
            {
                var outputName = "Response_" + sourcePath.Replace(".", "_");
                var displayName = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                    ?? property.Name.Humanize(LetterCasing.Title);

                if (!string.IsNullOrEmpty(prefix))
                {
                    displayName = $"{prefix.Replace(".", " > ")} > {displayName}";
                }

                var outputDescriptor = new OutputDescriptor
                {
                    Name = outputName,
                    DisplayName = $"Response: {displayName}",
                    Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description,
                    Type = property.PropertyType.IsEnum ? typeof(string) : property.PropertyType,
                    IsSynthetic = true,
                    ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(outputName),
                    ValueSetter = (activity, value) => activity.SyntheticProperties[outputName] = value!
                };

                outputDescriptors.Add(outputDescriptor);
            }
        }

        return outputDescriptors;
    }

    private static bool IsComplexType(Type type)
    {
        if (type.IsPrimitive || type.IsEnum) return false;
        if (type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || 
            type == typeof(DateTimeOffset) || type == typeof(Guid) || type == typeof(TimeSpan)) return false;
        if (Nullable.GetUnderlyingType(type) != null) return false;
        if (IsCollectionType(type)) return false;
        return type.IsClass;
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
                genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(Dictionary<,>) ||
                genericDef == typeof(IDictionary<,>))
            {
                return true;
            }
        }
        return false;
    }

    private static string GetSimpleUIHintForType(Type type)
    {
        if (type == typeof(bool)) return InputUIHints.Checkbox;
        if (type.IsEnum) return InputUIHints.DropDown;
        if (type == typeof(string)) return InputUIHints.SingleLine;
        if (IsComplexType(type)) return InputUIHints.MultiLine;
        return InputUIHints.SingleLine;
    }

    /// <summary>
    /// Creates output descriptors from the response type properties for RPC activities.
    /// Output property names are prefixed with "Response_" to distinguish from input properties.
    /// </summary>
    private List<OutputDescriptor> CreateRpcOutputDescriptorsFromType(Type responseType)
    {
        var outputDescriptors = new List<OutputDescriptor>();
        var properties = responseType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (var property in properties)
        {
            var propertyDisplayName = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                ?? property.Name.Humanize(LetterCasing.Title);
            var propertyDescription = property.GetCustomAttribute<DescriptionAttribute>()?.Description;

            // For enum types, we use string as the underlying type
            var outputType = property.PropertyType.IsEnum ? typeof(string) : property.PropertyType;

            var outputDescriptor = new OutputDescriptor
            {
                Name = $"Response_{property.Name}",
                DisplayName = $"Response: {propertyDisplayName}",
                Description = propertyDescription,
                Type = outputType,
                IsSynthetic = true,
                ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault($"Response_{property.Name}"),
                ValueSetter = (activity, value) => activity.SyntheticProperties[$"Response_{property.Name}"] = value!
            };

            outputDescriptors.Add(outputDescriptor);
        }

        return outputDescriptors;
    }
}