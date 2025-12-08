using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Elsa.ServiceBus.MassTransit.Activities;

/// <summary>
/// A generic activity that sends a request message and waits for a response (RPC pattern).
/// Used by the <see cref="MassTransitActivityTypeProvider"/> for synchronous request-response operations.
/// </summary>
[Browsable(false)]
public class SendRequest : CodeActivity
{
    /// <inheritdoc />
    public SendRequest([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
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
    /// The timeout for the RPC operation.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The destination address for the RPC request (for cross-process RPC).
    /// </summary>
    public Uri? DestinationAddress { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<ILogger<SendRequest>>();
        logger.LogInformation("SendRequest: Starting RPC call for {RequestType} -> {ResponseType}", RequestType.Name, ResponseType.Name);
        
        var request = BuildRequestFromInputs(context);
        logger.LogInformation("SendRequest: Built request message: {Request}", System.Text.Json.JsonSerializer.Serialize(request));
        
        var response = await SendRequestAsync(context, request, logger);
        logger.LogInformation("SendRequest: Received response: {Response}", System.Text.Json.JsonSerializer.Serialize(response));
        
        SetResponseOutputs(context, response);
    }

    /// <summary>
    /// Sends the request and gets the response using MassTransit's request client.
    /// </summary>
    private async Task<object> SendRequestAsync(ActivityExecutionContext context, object request, ILogger logger)
    {
        var clientFactory = context.GetRequiredService<IClientFactory>();
        var requestClientType = typeof(IRequestClient<>).MakeGenericType(RequestType);
        object requestClient;

        // If DestinationAddress is specified, always create a new client with that address
        if (DestinationAddress != null)
        {
            logger.LogInformation("SendRequest: Creating IRequestClient<{RequestType}> with destination {DestinationAddress}", RequestType.Name, DestinationAddress);
            
            // Use CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
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
            // Try to get the registered IRequestClient<T> from DI first
            object? diClient = null;
            try
            {
                diClient = context.GetService(requestClientType);
                if (diClient != null)
                {
                    logger.LogInformation("SendRequest: Using DI-registered IRequestClient<{RequestType}>", RequestType.Name);
                }
            }
            catch
            {
                // Ignore - will fall back to IClientFactory
            }

            if (diClient != null)
            {
                requestClient = diClient;
            }
            else
            {
                logger.LogInformation("SendRequest: IRequestClient<{RequestType}> not found in DI, creating via IClientFactory", RequestType.Name);
                
                var createClientMethod = typeof(IClientFactory)
                    .GetMethods()
                    .FirstOrDefault(m => 
                        m.Name == "CreateRequestClient" && 
                        m.IsGenericMethod &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(RequestTimeout));

                if (createClientMethod == null)
                {
                    createClientMethod = typeof(IClientFactory)
                        .GetMethods()
                        .FirstOrDefault(m => 
                            m.Name == "CreateRequestClient" && 
                            m.IsGenericMethod &&
                            m.GetParameters().Length == 0);
                }

                if (createClientMethod == null)
                {
                    throw new InvalidOperationException($"Could not find CreateRequestClient method on IClientFactory");
                }

                var genericCreateClient = createClientMethod.MakeGenericMethod(RequestType);
                
                if (createClientMethod.GetParameters().Length == 1)
                {
                    var timeout = RequestTimeout.After(s: (int)Timeout.TotalSeconds);
                    requestClient = genericCreateClient.Invoke(clientFactory, new object[] { timeout })!;
                }
                else
                {
                    requestClient = genericCreateClient.Invoke(clientFactory, Array.Empty<object>())!;
                }
            }
        }
        
        logger.LogInformation("SendRequest: Request client ready, invoking GetResponse");

        // Now call GetResponse on the request client
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

        logger.LogInformation("SendRequest: Calling GetResponse with {ArgCount} arguments", args.Count);

        var task = (Task)genericGetResponse.Invoke(requestClient, args.ToArray())!;
        await task.ConfigureAwait(false);

        logger.LogInformation("SendRequest: GetResponse completed");

        // Get the result from Response<TResponse>
        var resultProperty = task.GetType().GetProperty("Result");
        var responseWrapper = resultProperty!.GetValue(task);
        
        // Extract the Message property from Response<TResponse>
        var messageProperty = responseWrapper!.GetType().GetProperty("Message");
        return messageProperty!.GetValue(responseWrapper)!;
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

                // Check if the value is wrapped in Input<T>
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

            // Convert enum values to string for output
            var value = property.PropertyType.IsEnum && rawValue != null
                ? rawValue.ToString()
                : rawValue;

            // Check if an Output wrapper already exists in SyntheticProperties
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
                // Store the raw value in SyntheticProperties for later retrieval
                SyntheticProperties[$"Response_{property.Name}"] = value!;
            }
        }
    }

    /// <summary>
    /// Gets the value from an Input wrapper using the ActivityExecutionContext.
    /// </summary>
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
