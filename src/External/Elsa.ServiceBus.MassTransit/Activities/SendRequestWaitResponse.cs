using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using MassTransit;

namespace Elsa.ServiceBus.MassTransit.Activities;

/// <summary>
/// A generic activity that sends a request message and creates a bookmark to wait for the response asynchronously.
/// Used by the <see cref="MassTransitActivityTypeProvider"/> for asynchronous request-response operations.
/// </summary>
/// <remarks>
/// This activity is useful for long-running RPC operations where the workflow should not block
/// while waiting for the response. The workflow will be suspended and resumed when the response arrives.
/// </remarks>
[Browsable(false)]
public class SendRequestWaitResponse : Trigger<object>
{
    internal const string ResponseInputKey = "RpcResponse";
    internal const string CorrelationIdKey = "RpcCorrelationId";

    /// <inheritdoc />
    public SendRequestWaitResponse([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The request message type.
    /// </summary>
    public Type RequestType { get; set; } = null!;

    /// <summary>
    /// The response message type.
    /// </summary>
    public Type ResponseType { get; set; } = null!;

    /// <summary>
    /// The timeout for the RPC operation. If the response is not received within this time,
    /// the workflow will continue with a timeout error.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The destination address for the RPC request (for cross-process RPC).
    /// </summary>
    public Uri? DestinationAddress { get; set; }

    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context) => GetBookmarkPayload(context.ExpressionExecutionContext);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Check if we already have a response (resuming from bookmark)
        if (TryGetResponse(context, out var response))
        {
            await ExecuteInternalAsync(context, response);
            return;
        }

        // Build and send the request
        var request = BuildRequestFromInputs(context);
        await SendRequestAsync(context, request);

        // Check if response was received synchronously (from IRequestClient)
        if (TryGetResponse(context, out response))
        {
            await ExecuteInternalAsync(context, response);
            return;
        }

        // If no response yet, create bookmark to wait (this shouldn't happen with IRequestClient)
        var correlationId = context.GetProperty<Guid>(CorrelationIdKey);
        context.CreateBookmark(
            GetBookmarkPayload(context.ExpressionExecutionContext, correlationId),
            ResumeAsync,
            includeActivityInstanceId: false);
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        if (!TryGetResponse(context, out var response))
            throw new InvalidOperationException("Response was not received.");

        await ExecuteInternalAsync(context, response);
    }

    private ValueTask ExecuteInternalAsync(ActivityExecutionContext context, object response)
    {
        // Provide the received response as output
        context.Set(Result, response);

        // Set each property of the response as an individual output
        SetResponseOutputs(context, response);

        // Remove the input to prevent it from being passed to the next activity
        context.WorkflowInput.Remove(ResponseInputKey);

        return context.CompleteActivityAsync();
    }

    /// <summary>
    /// Sends the request and returns the correlation ID for tracking the response.
    /// </summary>
    private async Task<Guid> SendRequestAsync(ActivityExecutionContext context, object request)
    {
        var clientFactory = context.GetRequiredService<IClientFactory>();
        var correlationId = Guid.NewGuid();

        // Create request client with destination address if specified
        object requestClient;
        var requestClientType = typeof(IRequestClient<>).MakeGenericType(RequestType);

        if (DestinationAddress != null)
        {
            var createClientMethod = typeof(IClientFactory)
                .GetMethods()
                .FirstOrDefault(m => 
                    m.Name == "CreateRequestClient" && 
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[0].ParameterType == typeof(Uri) &&
                    m.GetParameters()[1].ParameterType == typeof(RequestTimeout));

            if (createClientMethod == null)
            {
                throw new InvalidOperationException($"Could not find CreateRequestClient(Uri, RequestTimeout) method on IClientFactory");
            }

            var genericCreateClient = createClientMethod.MakeGenericMethod(RequestType);
            var timeout = RequestTimeout.After(s: (int)Timeout.TotalSeconds);
            requestClient = genericCreateClient.Invoke(clientFactory, new object[] { DestinationAddress, timeout })!;
        }
        else
        {
            var createClientMethod = typeof(IClientFactory)
                .GetMethods()
                .FirstOrDefault(m => 
                    m.Name == "CreateRequestClient" && 
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType == typeof(RequestTimeout));

            if (createClientMethod == null)
            {
                throw new InvalidOperationException($"Could not find CreateRequestClient method on IClientFactory");
            }

            var genericCreateClient = createClientMethod.MakeGenericMethod(RequestType);
            var timeout = RequestTimeout.After(s: (int)Timeout.TotalSeconds);
            requestClient = genericCreateClient.Invoke(clientFactory, new object[] { timeout })!;
        }

        // Call GetResponse on the request client
        var getResponseMethod = requestClientType.GetMethods()
            .Where(m => m.Name == "GetResponse" && m.IsGenericMethod)
            .FirstOrDefault(m => m.GetParameters().Length >= 1);

        if (getResponseMethod == null)
        {
            throw new InvalidOperationException($"Could not find GetResponse method on IRequestClient<{RequestType.Name}>");
        }

        var genericGetResponse = getResponseMethod.MakeGenericMethod(ResponseType);
        
        var methodParams = genericGetResponse.GetParameters();
        var args = new List<object> { request };
        
        foreach (var param in methodParams.Skip(1))
        {
            if (param.ParameterType == typeof(CancellationToken))
            {
                args.Add(context.CancellationToken);
            }
            else if (param.ParameterType == typeof(RequestTimeout))
            {
                args.Add(RequestTimeout.After(s: (int)Timeout.TotalSeconds));
            }
            else if (param.HasDefaultValue)
            {
                args.Add(param.DefaultValue!);
            }
        }

        var task = (Task)genericGetResponse.Invoke(requestClient, args.ToArray())!;
        await task.ConfigureAwait(false);

        // Get the result from Response<TResponse>
        var resultProperty = task.GetType().GetProperty("Result");
        var responseWrapper = resultProperty!.GetValue(task);
        
        // Extract the Message property from Response<TResponse>
        var messageProperty = responseWrapper!.GetType().GetProperty("Message");
        var response = messageProperty!.GetValue(responseWrapper)!;

        // Store response for immediate use (no bookmark needed for sync RPC)
        context.SetProperty(ResponseInputKey, response);

        return correlationId;
    }

    /// <summary>
    /// Builds a request message instance from the dynamic input properties.
    /// </summary>
    private object BuildRequestFromInputs(ActivityExecutionContext context)
    {
        var message = Activator.CreateInstance(RequestType)!;

        var properties = RequestType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var property in properties)
        {
            if (SyntheticProperties.TryGetValue(property.Name, out var storedValue) && storedValue != null)
            {
                object? actualValue = null;
                var storedValueType = storedValue.GetType();

                if (storedValueType.IsGenericType && storedValueType.GetGenericTypeDefinition() == typeof(Input<>))
                {
                    var inputValueType = property.PropertyType.IsEnum ? typeof(string) : property.PropertyType;
                    var rawValue = GetValueFromInput(context, storedValue, inputValueType);

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
    /// Sets each property of the response as an individual output.
    /// </summary>
    private void SetResponseOutputs(ActivityExecutionContext context, object response)
    {
        var properties = ResponseType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (var property in properties)
        {
            var rawValue = property.GetValue(response);

            var value = property.PropertyType.IsEnum && rawValue != null
                ? rawValue.ToString()
                : rawValue;

            if (SyntheticProperties.TryGetValue($"Response_{property.Name}", out var existingOutput) && existingOutput != null)
            {
                var outputObj = existingOutput as Output;
                if (outputObj != null)
                {
                    context.Set(outputObj, value);
                }
            }
            else
            {
                SyntheticProperties[$"Response_{property.Name}"] = value!;
            }
        }
    }

    private bool TryGetResponse(ActivityExecutionContext context, out object response)
    {
        // First check workflow input (for bookmark resume)
        if (context.TryGetWorkflowInput(ResponseInputKey, out response))
        {
            if (response.GetType() == ResponseType)
                return true;
        }

        // Then check activity property (for synchronous RPC response)
        var propertyResponse = context.GetProperty<object?>(ResponseInputKey);
        if (propertyResponse != null && propertyResponse.GetType() == ResponseType)
        {
            response = propertyResponse;
            return true;
        }

        response = null!;
        return false;
    }

    private object GetBookmarkPayload(ExpressionExecutionContext context, Guid? correlationId = null)
    {
        return new RpcResponseBookmarkPayload(RequestType, ResponseType, correlationId);
    }

    private static object? GetValueFromInput(ActivityExecutionContext context, object inputWrapper, Type targetType)
    {
        try
        {
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

/// <summary>
/// Bookmark payload for RPC response waiting.
/// </summary>
internal record RpcResponseBookmarkPayload(Type RequestType, Type ResponseType, Guid? CorrelationId);
