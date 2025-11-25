using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.Workflows;
using Elsa.Workflows.Models;
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
        var message = Activator.CreateInstance(MessageType)!;
        
        var properties = MessageType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var property in properties)
        {
            if (SyntheticProperties.TryGetValue(property.Name, out var storedValue) && storedValue != null)
            {
                object? actualValue = null;
                var storedValueType = storedValue.GetType();
                
                // Check if the value is wrapped in Input<T>
                if (storedValueType.IsGenericType && storedValueType.GetGenericTypeDefinition() == typeof(Input<>))
                {
                    // For enum properties, the input type is string, so we need to get string first then convert
                    var inputValueType = property.PropertyType.IsEnum ? typeof(string) : property.PropertyType;
                    var rawValue = GetValueFromInput(context, storedValue, inputValueType);
                    
                    // If the property is an enum and we got a string, parse it
                    if (property.PropertyType.IsEnum && rawValue is string enumString && !string.IsNullOrEmpty(enumString))
                    {
                        actualValue = Enum.Parse(property.PropertyType, enumString);
                    }
                    else
                    {
                        actualValue = rawValue;
                    }
                }
                else
                {
                    // For enum properties, handle string to enum conversion
                    if (property.PropertyType.IsEnum && storedValue is string enumString && !string.IsNullOrEmpty(enumString))
                    {
                        actualValue = Enum.Parse(property.PropertyType, enumString);
                    }
                    else
                    {
                        actualValue = storedValue.ConvertTo(property.PropertyType);
                    }
                }

                if (actualValue != null)
                {
                    property.SetValue(message, actualValue);
                }
            }
        }

        return message;
    }
    
    /// <summary>
    /// Gets the value from an Input wrapper using the ActivityExecutionContext.
    /// </summary>
    private static object? GetValueFromInput(ActivityExecutionContext context, object inputWrapper, Type targetType)
    {
        try
        {
            // Use the ActivityExecutionContext.Get<T>(Input<T>) instance method
            var getMethod = typeof(ActivityExecutionContext)
                .GetMethods()
                .Where(m => m.Name == "Get" && m.IsGenericMethod)
                .FirstOrDefault(m => 
                {
                    var parameters = m.GetParameters();
                    if (parameters.Length != 1) return false;
                    var paramType = parameters[0].ParameterType;
                    return paramType.IsGenericType && 
                           paramType.GetGenericTypeDefinition() == typeof(Input<>);
                });

            if (getMethod != null)
            {
                var genericGetMethod = getMethod.MakeGenericMethod(targetType);
                return genericGetMethod.Invoke(context, new object[] { inputWrapper });
            }
            
            // Fallback: try to get value from Expression property directly
            var expressionProperty = inputWrapper.GetType().GetProperty("Expression");
            if (expressionProperty != null)
            {
                var expression = expressionProperty.GetValue(inputWrapper);
                if (expression != null)
                {
                    var valueProperty = expression.GetType().GetProperty("Value");
                    if (valueProperty != null)
                    {
                        var value = valueProperty.GetValue(expression);
                        return value?.ConvertTo(targetType);
                    }
                }
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
}