using System.ComponentModel;
using System.Reflection;
using Elsa.Extensions;
using Elsa.ServiceBus.MassTransit.Activities;
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
        var descriptors = CreateDescriptors(messageTypes);
        return new ValueTask<IEnumerable<ActivityDescriptor>>(descriptors.ToList());
    }

    private IEnumerable<ActivityDescriptor> CreateDescriptors(IEnumerable<Type> messageTypes)
    {
        var descriptors = new List<ActivityDescriptor>();
        foreach (var messageType in messageTypes)
        {
            descriptors.Add(CreateMessageReceivedDescriptor(messageType));
            
            if(messageType.IsClass)
                descriptors.Add(CreatePublishMessageDescriptor(messageType));
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
}