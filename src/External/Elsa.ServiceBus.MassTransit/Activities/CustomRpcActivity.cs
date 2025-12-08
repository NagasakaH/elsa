using System.Reflection;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.ServiceBus.MassTransit.Builders;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Elsa.ServiceBus.MassTransit.Activities;

/// <summary>
/// A custom RPC activity that uses fluent builder definitions to construct requests and map responses.
/// Uses outcome-based branching for success, timeout, and error scenarios.
/// </summary>
/// <remarks>
/// This activity supports three outcomes:
/// - Done: RPC call completed successfully
/// - Timeout: RPC call timed out
/// - Error: RPC call failed with an error
/// 
/// Workflow designers can connect different activities to each outcome port to handle
/// success and failure scenarios appropriately.
/// </remarks>
public class CustomRpcActivity : Activity
{
    /// <summary>
    /// The full type name for the activity.
    /// </summary>
    public new string Type { get; set; } = default!;

    /// <summary>
    /// The request type.
    /// </summary>
    public Type RequestType { get; set; } = default!;

    /// <summary>
    /// The response type.
    /// </summary>
    public Type ResponseType { get; set; } = default!;

    /// <summary>
    /// The timeout duration for the RPC call.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The destination address for the RPC call.
    /// </summary>
    public Uri? DestinationAddress { get; set; }

    /// <summary>
    /// Input definitions from the builder.
    /// </summary>
    public IList<ActivityInputDefinition> InputDefinitions { get; set; } = new List<ActivityInputDefinition>();

    /// <summary>
    /// Output definitions from the builder.
    /// </summary>
    public IList<ActivityOutputDefinition> OutputDefinitions { get; set; } = new List<ActivityOutputDefinition>();

    /// <summary>
    /// Custom request builder function.
    /// </summary>
    public Func<IDictionary<string, object?>, object>? RequestBuilder { get; set; }

    /// <summary>
    /// Custom response mapper function.
    /// </summary>
    public Func<object, IDictionary<string, object?>>? ResponseMapper { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<ILogger<CustomRpcActivity>>();
        var clientFactory = context.GetRequiredService<IClientFactory>();

        // Build the request object from input values
        var inputValues = GatherInputValues(context);
        var requestObject = BuildRequest(inputValues);

        logger.LogInformation("Sending RPC request: {RequestType}", RequestType.Name);

        // Execute RPC call with timeout handling
        try
        {
            object response;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
            cts.CancelAfter(Timeout);

            if (DestinationAddress != null)
            {
                response = await SendRequestWithDestination(clientFactory, DestinationAddress, requestObject, cts.Token);
            }
            else
            {
                response = await SendRequestWithoutDestination(clientFactory, requestObject, cts.Token);
            }

            logger.LogInformation("RPC request completed successfully: {ResponseType}", ResponseType.Name);
            
            // Map response to outputs
            MapResponseToOutputs(context, response);

            // Set success outputs
            context.Activity.SyntheticProperties["IsSuccess"] = true;
            context.Activity.SyntheticProperties["ErrorType"] = string.Empty;
            context.Activity.SyntheticProperties["ErrorMessage"] = string.Empty;
            context.Activity.SyntheticProperties["ErrorDetails"] = string.Empty;

            // Complete with Done outcome
            await context.CompleteActivityWithOutcomesAsync("Done");
        }
        catch (RequestTimeoutException ex)
        {
            logger.LogWarning(ex, "RPC request timed out after {Timeout}", Timeout);
            SetErrorOutputs(context, RpcErrorType.Timeout, 
                $"RPC request timed out after {Timeout.TotalSeconds} seconds", 
                ex.Message);
            await context.CompleteActivityWithOutcomesAsync("Timeout");
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken != context.CancellationToken)
        {
            logger.LogWarning(ex, "RPC request timed out (cancelled) after {Timeout}", Timeout);
            SetErrorOutputs(context, RpcErrorType.Timeout,
                $"RPC request timed out after {Timeout.TotalSeconds} seconds",
                ex.Message);
            await context.CompleteActivityWithOutcomesAsync("Timeout");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RPC request failed with error");
            SetErrorOutputs(context, RpcErrorType.Error, ex.Message, ex.ToString());
            await context.CompleteActivityWithOutcomesAsync("Error");
        }
    }

    private void SetErrorOutputs(ActivityExecutionContext context, RpcErrorType errorType, string errorMessage, string? errorDetails)
    {
        context.Activity.SyntheticProperties["IsSuccess"] = false;
        context.Activity.SyntheticProperties["ErrorType"] = errorType.ToString();
        context.Activity.SyntheticProperties["ErrorMessage"] = errorMessage;
        context.Activity.SyntheticProperties["ErrorDetails"] = errorDetails ?? string.Empty;
    }

    private Dictionary<string, object?> GatherInputValues(ActivityExecutionContext context)
    {
        var inputValues = new Dictionary<string, object?>();

        foreach (var inputDef in InputDefinitions)
        {
            var value = context.Activity.SyntheticProperties.GetValueOrDefault(inputDef.Name);

            // Handle JSON input for collections
            if (inputDef.CollectionStrategy == CollectionInputStrategy.JsonInput && value is string jsonString)
            {
                try
                {
                    var itemType = GetCollectionItemType(inputDef.Type);
                    var listType = typeof(List<>).MakeGenericType(itemType);
                    value = JsonSerializer.Deserialize(jsonString, listType, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException)
                {
                    // Keep as string if parsing fails
                }
            }

            inputValues[inputDef.Name] = value;
        }

        return inputValues;
    }

    private object BuildRequest(Dictionary<string, object?> inputValues)
    {
        // If custom builder is provided, use it
        if (RequestBuilder != null)
        {
            return RequestBuilder(inputValues);
        }

        // Otherwise, create instance and populate properties
        var request = Activator.CreateInstance(RequestType)!;

        foreach (var inputDef in InputDefinitions)
        {
            if (!inputValues.TryGetValue(inputDef.Name, out var value) || value == null)
                continue;

            var sourcePath = inputDef.SourcePath ?? inputDef.Name;
            SetPropertyByPath(request, sourcePath, value);
        }

        return request;
    }

    private void SetPropertyByPath(object target, string path, object value)
    {
        var parts = path.Split('.');
        object? currentTarget = target;

        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (currentTarget == null) return;
            
            var property = currentTarget.GetType().GetProperty(parts[i]);
            if (property == null) return;

            var propertyValue = property.GetValue(currentTarget);
            if (propertyValue == null)
            {
                propertyValue = Activator.CreateInstance(property.PropertyType);
                property.SetValue(currentTarget, propertyValue);
            }

            currentTarget = propertyValue;
        }

        if (currentTarget == null) return;
        
        var finalProperty = currentTarget.GetType().GetProperty(parts[^1]);
        if (finalProperty != null && finalProperty.CanWrite)
        {
            var convertedValue = ConvertValue(value, finalProperty.PropertyType);
            finalProperty.SetValue(currentTarget, convertedValue);
        }
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null) return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType.IsAssignableFrom(value.GetType()))
            return value;

        if (underlyingType.IsEnum && value is string strValue)
            return Enum.Parse(underlyingType, strValue, true);

        if (underlyingType == typeof(Guid) && value is string guidStr)
            return Guid.Parse(guidStr);

        return Convert.ChangeType(value, underlyingType);
    }

    private async Task<object> SendRequestWithDestination(
        IClientFactory clientFactory,
        Uri destinationUri,
        object requestObject,
        CancellationToken cancellationToken)
    {
        // Use reflection to call the generic CreateRequestClient<T>(Uri, RequestTimeout) method
        var clientFactoryType = typeof(IClientFactory);
        var createRequestClientMethod = clientFactoryType.GetMethods()
            .FirstOrDefault(m => m.Name == "CreateRequestClient" &&
                                m.IsGenericMethod &&
                                m.GetParameters().Length == 2 &&
                                m.GetParameters()[0].ParameterType == typeof(Uri) &&
                                m.GetParameters()[1].ParameterType == typeof(RequestTimeout));

        if (createRequestClientMethod == null)
        {
            throw new InvalidOperationException("Could not find CreateRequestClient(Uri, RequestTimeout) method on IClientFactory");
        }

        var genericCreateClient = createRequestClientMethod.MakeGenericMethod(RequestType);
        var timeout = RequestTimeout.After(s: (int)Timeout.TotalSeconds);
        var requestClient = genericCreateClient.Invoke(clientFactory, new object[] { destinationUri, timeout })!;

        // Get the GetResponse method from IRequestClient<T>
        var requestClientType = typeof(IRequestClient<>).MakeGenericType(RequestType);
        var getResponseMethod = requestClientType.GetMethods()
            .Where(m => m.Name == "GetResponse" && m.IsGenericMethod)
            .FirstOrDefault(m =>
            {
                var parameters = m.GetParameters();
                return parameters.Length >= 1;
            });

        if (getResponseMethod == null)
        {
            throw new InvalidOperationException($"Could not find GetResponse method on IRequestClient<{RequestType.Name}>");
        }

        var genericGetResponse = getResponseMethod.MakeGenericMethod(ResponseType);
        
        // Build arguments based on method signature
        var methodParams = genericGetResponse.GetParameters();
        var args = new List<object> { requestObject };
        
        foreach (var param in methodParams.Skip(1))
        {
            if (param.ParameterType == typeof(CancellationToken))
            {
                args.Add(cancellationToken);
            }
            else if (param.ParameterType == typeof(RequestTimeout))
            {
                args.Add(timeout);
            }
            else if (param.HasDefaultValue)
            {
                args.Add(param.DefaultValue!);
            }
        }

        var task = (Task)genericGetResponse.Invoke(requestClient, args.ToArray())!;
        await task.ConfigureAwait(false);

        // Get result
        var resultProperty = task.GetType().GetProperty("Result");
        var responseWrapper = resultProperty!.GetValue(task);

        // Get Message property from Response<T>
        var messageProperty = responseWrapper!.GetType().GetProperty("Message");
        return messageProperty!.GetValue(responseWrapper)!;
    }

    private async Task<object> SendRequestWithoutDestination(
        IClientFactory clientFactory,
        object requestObject,
        CancellationToken cancellationToken)
    {
        // Use IClientFactory directly - CreateRequestClient<T>(RequestTimeout) method
        var clientFactoryType = typeof(IClientFactory);
        
        // Look for CreateRequestClient with RequestTimeout parameter
        var createRequestClientMethod = clientFactoryType
            .GetMethods()
            .FirstOrDefault(m => m.Name == "CreateRequestClient" && 
                                m.IsGenericMethod &&
                                m.GetParameters().Length == 1 &&
                                m.GetParameters()[0].ParameterType == typeof(RequestTimeout));
        
        // Fall back to parameterless version
        if (createRequestClientMethod == null)
        {
            createRequestClientMethod = clientFactoryType
                .GetMethods()
                .FirstOrDefault(m => m.Name == "CreateRequestClient" && 
                                    m.IsGenericMethod &&
                                    m.GetParameters().Length == 0);
        }
        
        if (createRequestClientMethod == null)
        {
            throw new InvalidOperationException(
                $"Could not find CreateRequestClient method on IClientFactory. " +
                $"Available methods: {string.Join(", ", clientFactoryType.GetMethods().Select(m => m.Name))}");
        }
        
        var genericCreateClient = createRequestClientMethod.MakeGenericMethod(RequestType);
        var timeout = RequestTimeout.After(s: (int)Timeout.TotalSeconds);
        
        // Invoke the method
        object requestClient;
        if (createRequestClientMethod.GetParameters().Length == 1)
        {
            requestClient = genericCreateClient.Invoke(clientFactory, new object[] { timeout })!;
        }
        else
        {
            requestClient = genericCreateClient.Invoke(clientFactory, Array.Empty<object>())!;
        }

        var requestClientType = typeof(IRequestClient<>).MakeGenericType(RequestType);
        
        // Find GetResponse method
        var getResponseMethod = requestClientType.GetMethods()
            .Where(m => m.Name == "GetResponse" && m.IsGenericMethod)
            .FirstOrDefault(m =>
            {
                var parameters = m.GetParameters();
                return parameters.Length >= 1;
            });
        
        if (getResponseMethod == null)
        {
            throw new InvalidOperationException(
                $"Could not find GetResponse method on IRequestClient<{RequestType.Name}>. " +
                $"Available methods: {string.Join(", ", requestClientType.GetMethods().Select(m => $"{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})"))}");
        }
        
        var genericGetResponse = getResponseMethod.MakeGenericMethod(ResponseType);
        
        // Build arguments based on method signature
        var methodParams = genericGetResponse.GetParameters();
        var args = new List<object> { requestObject };
        
        foreach (var param in methodParams.Skip(1))
        {
            if (param.ParameterType == typeof(CancellationToken))
            {
                args.Add(cancellationToken);
            }
            else if (param.ParameterType == typeof(RequestTimeout))
            {
                args.Add(timeout);
            }
            else if (param.HasDefaultValue)
            {
                args.Add(param.DefaultValue!);
            }
        }

        var task = (Task)genericGetResponse.Invoke(requestClient, args.ToArray())!;
        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty("Result");
        var responseWrapper = resultProperty!.GetValue(task);
        var messageProperty = responseWrapper!.GetType().GetProperty("Message");
        return messageProperty!.GetValue(responseWrapper)!;
    }

    private void MapResponseToOutputs(ActivityExecutionContext context, object response)
    {
        // Store the full response
        context.Activity.SyntheticProperties["Response"] = response;
        
        // If custom mapper is provided, use it
        if (ResponseMapper != null)
        {
            var mappedValues = ResponseMapper(response);
            foreach (var kvp in mappedValues)
            {
                context.Activity.SyntheticProperties[kvp.Key] = kvp.Value!;
            }
            return;
        }

        // Otherwise, auto-map from definitions
        foreach (var outputDef in OutputDefinitions)
        {
            var sourcePath = outputDef.SourcePath ?? outputDef.Name.Replace("Response_", "");
            var value = GetPropertyByPath(response, sourcePath);
            context.Activity.SyntheticProperties[outputDef.Name] = value!;
        }
    }

    private static object? GetPropertyByPath(object target, string path)
    {
        var parts = path.Split('.');
        var current = target;

        foreach (var part in parts)
        {
            if (current == null) return null;
            var property = current.GetType().GetProperty(part);
            if (property == null) return null;
            current = property.GetValue(current);
        }

        return current;
    }

    private static Type GetCollectionItemType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType()!;

        if (collectionType.IsGenericType)
        {
            var genericArgs = collectionType.GetGenericArguments();
            return genericArgs[0];
        }

        return typeof(object);
    }
}

/// <summary>
/// RPC error types for branching.
/// </summary>
public enum RpcErrorType
{
    /// <summary>
    /// The RPC call timed out.
    /// </summary>
    Timeout,
    
    /// <summary>
    /// The RPC call failed with an error.
    /// </summary>
    Error
}

/// <summary>
/// Information about an RPC error.
/// </summary>
public class RpcErrorInfo
{
    /// <summary>
    /// The type of error that occurred.
    /// </summary>
    public RpcErrorType ErrorType { get; set; }
    
    /// <summary>
    /// A human-readable error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional details about the error.
    /// </summary>
    public string? Details { get; set; }
}
