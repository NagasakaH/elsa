using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using MassTransit;

namespace Elsa.ServiceBus.MassTransit.Activities;

/// <summary>
/// A generic activity that publishes a message of a given type. Used by the <see cref="MassTransitActivityTypeProvider"/>.
/// </summary>
[Browsable(false)]
public class PublishMessage : CodeActivity
{
    /// <inheritdoc />
    public PublishMessage([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The message type to publish.
    /// </summary>
    public Type MessageType { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var bus = context.GetRequiredService<IBus>();
        var message = BuildMessageFromInputs(context);
        await bus.Publish(message, context.CancellationToken);
    }

    /// <summary>
    /// Builds a message instance from the dynamic input properties.
    /// </summary>
    private object BuildMessageFromInputs(ActivityExecutionContext context)
    {
        // Create an instance of the message type
        var message = Activator.CreateInstance(MessageType)!;
        
        var properties = MessageType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var property in properties)
        {
            // Try to get the input value for this property from SyntheticProperties
            if (SyntheticProperties.TryGetValue(property.Name, out var inputValue) && inputValue != null)
            {
                // Convert the value to the property type if necessary
                var convertedValue = inputValue.ConvertTo(property.PropertyType);
                property.SetValue(message, convertedValue);
            }
        }

        return message;
    }
}