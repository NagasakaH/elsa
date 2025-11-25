using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.Workflows;
using Elsa.Workflows.Models;

namespace Elsa.ServiceBus.MassTransit.Activities;

/// <summary>
/// A generic activity that waits for a message of a given type to be received. Used by the <see cref="MassTransitActivityTypeProvider"/>.
/// </summary>
[Browsable(false)]
public class MessageReceived : Trigger<object>
{
    internal const string InputKey = "Message";

    /// <inheritdoc />
    public MessageReceived([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The message type to receive.
    /// </summary>
    public Type MessageType { get; set; } = null!;

    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context) => GetBookmarkPayload(context.ExpressionExecutionContext);

    /// <inheritdoc />
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // If we did not receive external input, create a bookmark and wait.
        if (!TryGetMessage(context, out var message))
        {
            context.CreateBookmark(GetBookmarkPayload(context.ExpressionExecutionContext), ResumeAsync, includeActivityInstanceId: false);
            return default;
        }

        return ExecuteInternalAsync(context, message);
    }

    private ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        if (!TryGetMessage(context, out var message))
            throw new InvalidOperationException("Message was not received.");
        return ExecuteInternalAsync(context, message);
    }

    private ValueTask ExecuteInternalAsync(ActivityExecutionContext context, object message)
    {
        // Provide the received message as output (for backward compatibility).
        context.Set(Result, message);

        // Set each property of the message as an individual output.
        SetPropertyOutputs(context, message);

        // Remove the input to prevent it from being passed to the next activity.
        context.WorkflowInput.Remove(InputKey);

        return context.CompleteActivityAsync();
    }

    /// <summary>
    /// Sets each property of the message as an individual output.
    /// </summary>
    private void SetPropertyOutputs(ActivityExecutionContext context, object message)
    {
        var properties = MessageType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (var property in properties)
        {
            var value = property.GetValue(message);
            
            // Check if an Output wrapper already exists in SyntheticProperties
            if (SyntheticProperties.TryGetValue(property.Name, out var existingOutput) && existingOutput != null)
            {
                var outputObj = existingOutput as Output;
                if (outputObj != null)
                {
                    context.Set(outputObj, value);
                }
            }
            else
            {
                // Store the raw value in SyntheticProperties for later retrieval
                SyntheticProperties[property.Name] = value!;
            }
        }
    }

    private bool TryGetMessage(ActivityExecutionContext context, out object message)
    {
        if (!context.TryGetWorkflowInput(InputKey, out message))
            return false;

        if (message.GetType() == MessageType)
            return true;

        message = null!;
        return false;
    }

    private object GetBookmarkPayload(ExpressionExecutionContext context)
    {
        return new MessageReceivedBookmarkPayload(MessageType);
    }
}

internal record MessageReceivedBookmarkPayload(Type MessageType);