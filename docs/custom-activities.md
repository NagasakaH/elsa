# Custom Activities Design

## Overview
This document describes the design and implementation of custom activities for the Elsa workflow system, including base classes, registration mechanisms, and development guidelines.

## Custom Activity Architecture

### Base Activity Classes

#### Core Activity Base Class
```csharp
public abstract class CustomActivityBase : CodeActivity
{
    protected CustomActivityBase()
    {
        // Set default properties
    }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        try
        {
            await ExecuteActivityAsync(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
            throw;
        }
    }

    protected abstract Task ExecuteActivityAsync(ActivityExecutionContext context);
    
    protected virtual Task HandleExceptionAsync(ActivityExecutionContext context, Exception exception)
    {
        // Default exception handling
        context.SetResult("Error", exception.Message);
        return Task.CompletedTask;
    }
}
```

#### Async Activity Base Class
```csharp
public abstract class CustomAsyncActivityBase : Activity
{
    protected CustomAsyncActivityBase()
    {
        CanStartWorkflow = false;
    }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Create bookmark for resumption
        var bookmark = context.CreateBookmark(OnResumeAsync);
        
        // Start async operation
        await StartAsyncOperationAsync(context);
        
        // Activity will be resumed when async operation completes
    }

    protected abstract Task StartAsyncOperationAsync(ActivityExecutionContext context);
    
    private async ValueTask OnResumeAsync(ActivityExecutionContext context)
    {
        await OnAsyncOperationCompleteAsync(context);
    }
    
    protected abstract Task OnAsyncOperationCompleteAsync(ActivityExecutionContext context);
}
```

### Property Definitions

#### Input Properties
```csharp
public class InputPropertyDefinition
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public Type Type { get; set; }
    public bool IsRequired { get; set; }
    public object DefaultValue { get; set; }
    public string Category { get; set; }
    public int Order { get; set; }
    public string[] SupportedSyntax { get; set; } = { "Literal", "JavaScript", "Liquid" };
}
```

#### Output Properties
```csharp
public class OutputPropertyDefinition
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public Type Type { get; set; }
    public string Category { get; set; }
    public int Order { get; set; }
}
```

### Activity Metadata

#### Activity Descriptor
```csharp
[AttributeUsage(AttributeTargets.Class)]
public class ActivityDescriptorAttribute : Attribute
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Category { get; set; } = "Custom";
    public string Icon { get; set; }
    public bool IsBrowsable { get; set; } = true;
    public ActivityKind Kind { get; set; } = ActivityKind.Action;
}

public enum ActivityKind
{
    Action,
    Trigger,
    Task,
    Event
}
```

## Custom Activity Registration

### Registration System

#### Activity Registry
```csharp
public interface ICustomActivityRegistry
{
    void RegisterActivity<T>() where T : class, IActivity;
    void RegisterActivity(Type activityType);
    void RegisterActivitiesFromAssembly(Assembly assembly);
    IEnumerable<ActivityDescriptor> GetActivityDescriptors();
    ActivityDescriptor GetActivityDescriptor(string name);
    Type GetActivityType(string name);
}

public class CustomActivityRegistry : ICustomActivityRegistry
{
    private readonly Dictionary<string, ActivityDescriptor> _activities = new();
    private readonly Dictionary<string, Type> _activityTypes = new();

    public void RegisterActivity<T>() where T : class, IActivity
    {
        RegisterActivity(typeof(T));
    }

    public void RegisterActivity(Type activityType)
    {
        var descriptor = CreateActivityDescriptor(activityType);
        _activities[descriptor.Name] = descriptor;
        _activityTypes[descriptor.Name] = activityType;
    }

    public void RegisterActivitiesFromAssembly(Assembly assembly)
    {
        var activityTypes = assembly.GetTypes()
            .Where(t => typeof(IActivity).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        foreach (var type in activityTypes)
        {
            RegisterActivity(type);
        }
    }

    private ActivityDescriptor CreateActivityDescriptor(Type activityType)
    {
        var attribute = activityType.GetCustomAttribute<ActivityDescriptorAttribute>();
        
        return new ActivityDescriptor
        {
            Name = attribute?.Name ?? activityType.Name,
            DisplayName = attribute?.DisplayName ?? activityType.Name,
            Description = attribute?.Description ?? string.Empty,
            Category = attribute?.Category ?? "Custom",
            Icon = attribute?.Icon ?? "fas fa-cog",
            IsBrowsable = attribute?.IsBrowsable ?? true,
            Kind = attribute?.Kind ?? ActivityKind.Action,
            Type = activityType,
            InputProperties = GetInputProperties(activityType),
            OutputProperties = GetOutputProperties(activityType)
        };
    }
}
```

#### Service Registration
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomActivities(
        this IServiceCollection services,
        Action<CustomActivityOptions> configure = null)
    {
        var options = new CustomActivityOptions();
        configure?.Invoke(options);

        services.AddSingleton<ICustomActivityRegistry, CustomActivityRegistry>();
        
        // Register custom activities
        var registry = new CustomActivityRegistry();
        
        foreach (var assembly in options.AssembliesToScan)
        {
            registry.RegisterActivitiesFromAssembly(assembly);
        }
        
        foreach (var activityType in options.ActivityTypesToRegister)
        {
            registry.RegisterActivity(activityType);
        }

        return services;
    }
}

public class CustomActivityOptions
{
    public List<Assembly> AssembliesToScan { get; set; } = new();
    public List<Type> ActivityTypesToRegister { get; set; } = new();
}
```

## Example Custom Activities

### 1. HTTP Request Activity

```csharp
[ActivityDescriptor(
    Name = "HttpRequest",
    DisplayName = "HTTP Request",
    Description = "Sends an HTTP request and returns the response",
    Category = "HTTP",
    Icon = "fas fa-globe"
)]
public class HttpRequestActivity : CustomActivityBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpRequestActivity(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [ActivityInput(
        Label = "URL",
        Hint = "The URL to send the request to",
        SupportedSyntax = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Literal }
    )]
    public Input<string> Url { get; set; } = default!;

    [ActivityInput(
        Label = "Method",
        Hint = "The HTTP method to use",
        DefaultValue = "GET",
        Options = new[] { "GET", "POST", "PUT", "DELETE", "PATCH" }
    )]
    public Input<string> Method { get; set; } = new("GET");

    [ActivityInput(
        Label = "Headers",
        Hint = "HTTP headers as JSON object"
    )]
    public Input<object> Headers { get; set; } = default!;

    [ActivityInput(
        Label = "Body",
        Hint = "Request body content"
    )]
    public Input<string> Body { get; set; } = default!;

    [ActivityOutput]
    public Output<string> Response { get; set; } = default!;

    [ActivityOutput]
    public Output<int> StatusCode { get; set; } = default!;

    [ActivityOutput]
    public Output<object> ResponseHeaders { get; set; } = default!;

    protected override async Task ExecuteActivityAsync(ActivityExecutionContext context)
    {
        var url = context.Get(Url);
        var method = context.Get(Method);
        var headers = context.Get(Headers);
        var body = context.Get(Body);

        var httpClient = _httpClientFactory.CreateClient();
        
        // Set headers
        if (headers != null)
        {
            var headerDict = JsonSerializer.Deserialize<Dictionary<string, string>>(
                JsonSerializer.Serialize(headers));
            
            foreach (var header in headerDict)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        // Create request
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        
        if (!string.IsNullOrEmpty(body))
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        // Send request
        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Set outputs
        context.Set(Response, responseContent);
        context.Set(StatusCode, (int)response.StatusCode);
        context.Set(ResponseHeaders, response.Headers.ToDictionary(h => h.Key, h => h.Value));
    }
}
```

### 2. Email Send Activity

```csharp
[ActivityDescriptor(
    Name = "SendEmail",
    DisplayName = "Send Email",
    Description = "Sends an email message",
    Category = "Email",
    Icon = "fas fa-envelope"
)]
public class SendEmailActivity : CustomActivityBase
{
    private readonly IEmailService _emailService;

    public SendEmailActivity(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [ActivityInput(
        Label = "To",
        Hint = "Recipient email address(es), separated by comma"
    )]
    public Input<string> To { get; set; } = default!;

    [ActivityInput(
        Label = "CC",
        Hint = "CC email address(es), separated by comma"
    )]
    public Input<string> Cc { get; set; } = default!;

    [ActivityInput(
        Label = "BCC",
        Hint = "BCC email address(es), separated by comma"
    )]
    public Input<string> Bcc { get; set; } = default!;

    [ActivityInput(
        Label = "Subject",
        Hint = "Email subject"
    )]
    public Input<string> Subject { get; set; } = default!;

    [ActivityInput(
        Label = "Body",
        Hint = "Email body content",
        UIHint = ActivityInputUIHints.MultiLine
    )]
    public Input<string> Body { get; set; } = default!;

    [ActivityInput(
        Label = "Is HTML",
        Hint = "Whether the body is HTML content",
        DefaultValue = false
    )]
    public Input<bool> IsHtml { get; set; } = new(false);

    [ActivityOutput]
    public Output<bool> Success { get; set; } = default!;

    [ActivityOutput]
    public Output<string> ErrorMessage { get; set; } = default!;

    protected override async Task ExecuteActivityAsync(ActivityExecutionContext context)
    {
        try
        {
            var to = context.Get(To);
            var cc = context.Get(Cc);
            var bcc = context.Get(Bcc);
            var subject = context.Get(Subject);
            var body = context.Get(Body);
            var isHtml = context.Get(IsHtml);

            var emailMessage = new EmailMessage
            {
                To = ParseEmailAddresses(to),
                Cc = ParseEmailAddresses(cc),
                Bcc = ParseEmailAddresses(bcc),
                Subject = subject,
                Body = body,
                IsHtml = isHtml
            };

            await _emailService.SendEmailAsync(emailMessage);
            
            context.Set(Success, true);
        }
        catch (Exception ex)
        {
            context.Set(Success, false);
            context.Set(ErrorMessage, ex.Message);
        }
    }

    private List<string> ParseEmailAddresses(string addresses)
    {
        if (string.IsNullOrWhiteSpace(addresses))
            return new List<string>();

        return addresses.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim())
            .Where(a => !string.IsNullOrEmpty(a))
            .ToList();
    }
}
```

### 3. Database Query Activity

```csharp
[ActivityDescriptor(
    Name = "DatabaseQuery",
    DisplayName = "Database Query",
    Description = "Executes a database query and returns results",
    Category = "Database",
    Icon = "fas fa-database"
)]
public class DatabaseQueryActivity : CustomActivityBase
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DatabaseQueryActivity(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    [ActivityInput(
        Label = "Connection String",
        Hint = "Database connection string"
    )]
    public Input<string> ConnectionString { get; set; } = default!;

    [ActivityInput(
        Label = "Query",
        Hint = "SQL query to execute",
        UIHint = ActivityInputUIHints.MultiLine
    )]
    public Input<string> Query { get; set; } = default!;

    [ActivityInput(
        Label = "Parameters",
        Hint = "Query parameters as JSON object"
    )]
    public Input<object> Parameters { get; set; } = default!;

    [ActivityOutput]
    public Output<List<Dictionary<string, object>>> Results { get; set; } = default!;

    [ActivityOutput]
    public Output<int> RowCount { get; set; } = default!;

    protected override async Task ExecuteActivityAsync(ActivityExecutionContext context)
    {
        var connectionString = context.Get(ConnectionString);
        var query = context.Get(Query);
        var parameters = context.Get(Parameters);

        using var connection = _connectionFactory.CreateConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = query;

        // Add parameters
        if (parameters != null)
        {
            var paramDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(parameters));

            foreach (var param in paramDict)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = param.Key;
                parameter.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        var results = new List<Dictionary<string, object>>();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }
            results.Add(row);
        }

        context.Set(Results, results);
        context.Set(RowCount, results.Count);
    }
}
```

### 4. File Operations Activity

```csharp
[ActivityDescriptor(
    Name = "FileOperation",
    DisplayName = "File Operation",
    Description = "Performs file system operations",
    Category = "File System",
    Icon = "fas fa-file"
)]
public class FileOperationActivity : CustomActivityBase
{
    [ActivityInput(
        Label = "Operation",
        Hint = "File operation to perform",
        Options = new[] { "Read", "Write", "Copy", "Move", "Delete", "Exists" }
    )]
    public Input<string> Operation { get; set; } = default!;

    [ActivityInput(
        Label = "Source Path",
        Hint = "Source file path"
    )]
    public Input<string> SourcePath { get; set; } = default!;

    [ActivityInput(
        Label = "Destination Path",
        Hint = "Destination file path (for copy/move operations)"
    )]
    public Input<string> DestinationPath { get; set; } = default!;

    [ActivityInput(
        Label = "Content",
        Hint = "Content to write (for write operation)",
        UIHint = ActivityInputUIHints.MultiLine
    )]
    public Input<string> Content { get; set; } = default!;

    [ActivityOutput]
    public Output<string> FileContent { get; set; } = default!;

    [ActivityOutput]
    public Output<bool> Success { get; set; } = default!;

    [ActivityOutput]
    public Output<bool> Exists { get; set; } = default!;

    protected override async Task ExecuteActivityAsync(ActivityExecutionContext context)
    {
        var operation = context.Get(Operation);
        var sourcePath = context.Get(SourcePath);
        var destinationPath = context.Get(DestinationPath);
        var content = context.Get(Content);

        try
        {
            switch (operation.ToLower())
            {
                case "read":
                    var fileContent = await File.ReadAllTextAsync(sourcePath);
                    context.Set(FileContent, fileContent);
                    context.Set(Success, true);
                    break;

                case "write":
                    await File.WriteAllTextAsync(sourcePath, content);
                    context.Set(Success, true);
                    break;

                case "copy":
                    File.Copy(sourcePath, destinationPath, true);
                    context.Set(Success, true);
                    break;

                case "move":
                    File.Move(sourcePath, destinationPath);
                    context.Set(Success, true);
                    break;

                case "delete":
                    File.Delete(sourcePath);
                    context.Set(Success, true);
                    break;

                case "exists":
                    var exists = File.Exists(sourcePath);
                    context.Set(Exists, exists);
                    context.Set(Success, true);
                    break;

                default:
                    throw new ArgumentException($"Unknown operation: {operation}");
            }
        }
        catch (Exception ex)
        {
            context.Set(Success, false);
            throw new ActivityExecutionException($"File operation failed: {ex.Message}", ex);
        }
    }
}
```

## Development Guidelines

### Best Practices

#### 1. Activity Design
- **Single Responsibility**: Each activity should have a single, well-defined purpose
- **Meaningful Names**: Use descriptive names for activities and properties
- **Clear Documentation**: Provide comprehensive descriptions and hints
- **Error Handling**: Implement proper error handling and recovery
- **Async Operations**: Use async/await patterns for I/O operations

#### 2. Property Design
- **Required vs Optional**: Clearly mark required properties
- **Default Values**: Provide sensible default values
- **Validation**: Implement input validation
- **Type Safety**: Use strongly typed properties
- **Syntax Support**: Support multiple syntax types (Literal, JavaScript, Liquid)

#### 3. Performance Considerations
- **Resource Management**: Properly dispose of resources
- **Cancellation Support**: Support cancellation tokens
- **Caching**: Cache expensive operations when appropriate
- **Memory Usage**: Be mindful of memory usage with large data sets
- **Connection Pooling**: Use connection pooling for database operations

#### 4. Testing
- **Unit Tests**: Write comprehensive unit tests
- **Integration Tests**: Test with real workflow execution
- **Error Scenarios**: Test error conditions and edge cases
- **Performance Tests**: Test with realistic data volumes
- **Mock Dependencies**: Use mocking for external dependencies

### Activity Categories

#### Standard Categories
- **Control Flow**: Activities that control workflow execution
- **Data**: Activities for data manipulation and transformation
- **HTTP**: Activities for HTTP requests and web services
- **Email**: Activities for email operations
- **File System**: Activities for file operations
- **Database**: Activities for database operations
- **Scheduling**: Activities for time-based operations
- **Integration**: Activities for third-party integrations
- **Business Logic**: Activities for custom business rules

### Registration Example

#### Startup Configuration
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddElsa(elsaOptions =>
    {
        elsaOptions
            .UseEntityFrameworkPersistence(ef => ef.UsePostgreSql(connectionString))
            .AddConsoleActivities()
            .AddHttpActivities()
            .AddEmailActivities()
            .AddTimerActivities()
            .AddFileActivities()
            .AddCustomActivities(options =>
            {
                options.AssembliesToScan.Add(typeof(HttpRequestActivity).Assembly);
                options.ActivityTypesToRegister.Add(typeof(SendEmailActivity));
                options.ActivityTypesToRegister.Add(typeof(DatabaseQueryActivity));
                options.ActivityTypesToRegister.Add(typeof(FileOperationActivity));
            });
    });

    // Register dependencies
    services.AddHttpClient();
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
}
```

## Activity Discovery and Metadata

### Activity Catalog
```csharp
public class ActivityCatalogService
{
    private readonly ICustomActivityRegistry _registry;

    public ActivityCatalogService(ICustomActivityRegistry registry)
    {
        _registry = registry;
    }

    public IEnumerable<ActivityCatalogItem> GetActivityCatalog()
    {
        return _registry.GetActivityDescriptors()
            .Where(d => d.IsBrowsable)
            .Select(d => new ActivityCatalogItem
            {
                Name = d.Name,
                DisplayName = d.DisplayName,
                Description = d.Description,
                Category = d.Category,
                Icon = d.Icon,
                Kind = d.Kind,
                InputProperties = d.InputProperties,
                OutputProperties = d.OutputProperties
            })
            .OrderBy(item => item.Category)
            .ThenBy(item => item.DisplayName);
    }
}
```

### Studio Integration
The custom activities will automatically appear in the Elsa Studio activity palette, organized by category. The Studio will use the metadata to:

- Display activities in the toolbox
- Show property editors with appropriate UI hints
- Validate required properties
- Generate activity documentation
- Provide intellisense for supported syntax types

## Versioning and Compatibility

### Activity Versioning
```csharp
[ActivityDescriptor(
    Name = "MyActivity",
    Version = "1.0.0"
)]
public class MyActivity : CustomActivityBase
{
    // Activity implementation
}
```

### Backward Compatibility
- Use versioned activity names when breaking changes are introduced
- Maintain support for older activity versions
- Provide migration utilities for workflow definitions
- Document breaking changes and migration paths

## Security Considerations

### Input Validation
- Validate all input parameters
- Sanitize user input to prevent injection attacks
- Implement proper authentication for external services
- Use secure connection strings and API keys

### Access Control
- Implement role-based access control for sensitive activities
- Audit activity execution for compliance
- Secure external service connections
- Encrypt sensitive configuration data

This design provides a comprehensive framework for creating, registering, and managing custom activities in the Elsa workflow system, with examples and best practices for common scenarios.
