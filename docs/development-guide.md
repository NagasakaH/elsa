# Development Guide

## Overview
This document provides comprehensive instructions for setting up and developing the Elsa workflow system, including environment setup, development workflows, and best practices.

## Prerequisites

### Required Software
- **.NET 8 SDK**: Latest version of .NET 8
- **PostgreSQL**: Version 13 or higher
- **Docker**: For development database (optional but recommended)
- **Git**: Version control
- **IDE**: Visual Studio 2022, VS Code, or JetBrains Rider

### Optional Tools
- **Docker Desktop**: For containerized development
- **pgAdmin**: PostgreSQL administration tool
- **Postman**: API testing
- **Azure Data Studio**: Database management

## Environment Setup

### 1. Clone Repository
```bash
git clone <repository-url>
cd elsa
```

### 2. Install .NET 8 SDK
```bash
# Check if .NET 8 is installed
dotnet --version

# If not installed, download from:
# https://dotnet.microsoft.com/download/dotnet/8.0
```

### 3. Database Setup

#### Option A: Docker PostgreSQL (Recommended)
```bash
# Start PostgreSQL container
docker run --name elsa-postgres \
  -e POSTGRES_DB=elsa_workflows \
  -e POSTGRES_USER=elsa_user \
  -e POSTGRES_PASSWORD=elsa_password \
  -p 5432:5432 \
  -d postgres:15

# Verify container is running
docker ps
```

#### Option B: Local PostgreSQL Installation
1. Install PostgreSQL from https://www.postgresql.org/download/
2. Create database and user:
```sql
CREATE DATABASE elsa_workflows;
CREATE USER elsa_user WITH PASSWORD 'elsa_password';
GRANT ALL PRIVILEGES ON DATABASE elsa_workflows TO elsa_user;
```

### 4. Environment Configuration

#### appsettings.Development.json
Create development configuration files for each project:

**Elsa.Studio.Host/appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Elsa": "Debug"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=elsa_password;Include Error Detail=true"
  },
  "Elsa": {
    "Studio": {
      "BaseUrl": "https://localhost:5001"
    },
    "Runtime": {
      "BaseUrl": "https://localhost:5002"
    }
  }
}
```

**Elsa.Workflow.Runtime/appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Elsa": "Debug"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=elsa_password;Include Error Detail=true"
  },
  "Elsa": {
    "Runtime": {
      "ApiKey": "dev-api-key-12345"
    }
  },
  "AllowedHosts": "*"
}
```

### 5. Build and Run

#### Restore Dependencies
```bash
# From repository root
dotnet restore
```

#### Database Migrations
```bash
# Navigate to shared project
cd src/Elsa.Shared

# Create initial migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update

# Verify migration
dotnet ef migrations list
```

#### Run Applications
```bash
# Terminal 1: Start Elsa Studio
cd src/Elsa.Studio.Host
dotnet run

# Terminal 2: Start Elsa Runtime
cd src/Elsa.Workflow.Runtime
dotnet run
```

### 6. Verify Installation
- **Elsa Studio**: https://localhost:5001
- **Elsa Runtime**: https://localhost:5002
- **Runtime Health Check**: https://localhost:5002/health
- **Runtime Swagger**: https://localhost:5002/swagger

## Project Structure

### Solution Structure
```
elsa/
├── src/
│   ├── Elsa.Studio.Host/          # Workflow designer web application
│   ├── Elsa.Workflow.Runtime/     # Workflow execution engine
│   ├── Elsa.Shared/               # Shared libraries and data access
│   └── Elsa.CustomActivities/     # Custom activity implementations
├── tests/
│   ├── Elsa.Studio.Host.Tests/
│   ├── Elsa.Workflow.Runtime.Tests/
│   ├── Elsa.Shared.Tests/
│   └── Elsa.CustomActivities.Tests/
├── docs/                          # Documentation
├── docker-compose.yml             # Development environment
├── .gitignore
├── README.md
└── elsa.sln                       # Solution file
```

### Key Directories

#### Elsa.Studio.Host
- **Controllers/**: Studio API controllers
- **Views/**: Blazor pages and components
- **wwwroot/**: Static assets (CSS, JS, images)
- **Program.cs**: Application entry point
- **Startup.cs**: Service configuration

#### Elsa.Workflow.Runtime
- **Controllers/**: Runtime API controllers
- **Services/**: Business logic services
- **Middleware/**: Custom middleware
- **Program.cs**: Application entry point

#### Elsa.Shared
- **Data/**: Entity Framework models and DbContext
- **Repositories/**: Data access layer
- **Services/**: Shared business logic
- **Extensions/**: Extension methods
- **Migrations/**: Database migrations

#### Elsa.CustomActivities
- **Activities/**: Custom activity implementations
- **Services/**: Activity-specific services
- **Models/**: Activity data models

## Development Workflow

### 1. Feature Development

#### Create Feature Branch
```bash
git checkout -b feature/activity-logging
```

#### Development Process
1. Write failing tests
2. Implement feature
3. Ensure tests pass
4. Update documentation
5. Create pull request

#### Code Structure Example
```csharp
// 1. Define interface
public interface IActivityLogger
{
    Task LogActivityExecutionAsync(string activityId, object data);
}

// 2. Implement service
public class ActivityLogger : IActivityLogger
{
    public async Task LogActivityExecutionAsync(string activityId, object data)
    {
        // Implementation
    }
}

// 3. Register service
services.AddScoped<IActivityLogger, ActivityLogger>();

// 4. Write tests
[Test]
public async Task LogActivityExecutionAsync_ShouldLogActivity()
{
    // Arrange, Act, Assert
}
```

### 2. Database Changes

#### Adding New Entity
```csharp
// 1. Create entity model
public class WorkflowLog
{
    public Guid Id { get; set; }
    public string WorkflowInstanceId { get; set; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
}

// 2. Add to DbContext
public DbSet<WorkflowLog> WorkflowLogs { get; set; }

// 3. Create migration
dotnet ef migrations add AddWorkflowLogs

// 4. Update database
dotnet ef database update
```

#### Migration Best Practices
- Use descriptive migration names
- Review generated SQL before applying
- Test migrations on development data
- Create rollback scripts for production

### 3. Custom Activity Development

#### Step-by-Step Process
```csharp
// 1. Create activity class
[ActivityDescriptor(
    Name = "LogMessage",
    DisplayName = "Log Message",
    Description = "Logs a message to the console",
    Category = "Logging"
)]
public class LogMessageActivity : CustomActivityBase
{
    [ActivityInput(Label = "Message")]
    public Input<string> Message { get; set; } = default!;

    protected override Task ExecuteActivityAsync(ActivityExecutionContext context)
    {
        var message = context.Get(Message);
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        return Task.CompletedTask;
    }
}

// 2. Register activity
services.AddCustomActivities(options =>
{
    options.ActivityTypesToRegister.Add(typeof(LogMessageActivity));
});

// 3. Write tests
[Test]
public async Task LogMessageActivity_ShouldLogMessage()
{
    // Test implementation
}
```

### 4. API Development

#### Adding New Endpoint
```csharp
// 1. Create controller
[ApiController]
[Route("api/v1/[controller]")]
public class WorkflowMetricsController : ControllerBase
{
    private readonly IWorkflowMetricsService _metricsService;

    public WorkflowMetricsController(IWorkflowMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMetrics()
    {
        var metrics = await _metricsService.GetMetricsAsync();
        return Ok(metrics);
    }
}

// 2. Implement service
public interface IWorkflowMetricsService
{
    Task<WorkflowMetrics> GetMetricsAsync();
}

// 3. Add API tests
[Test]
public async Task GetMetrics_ShouldReturnMetrics()
{
    // Integration test
}
```

## Testing

### Test Structure
```
tests/
├── Unit/                          # Unit tests
├── Integration/                   # Integration tests
├── E2E/                          # End-to-end tests
└── Common/                       # Test utilities
```

### Unit Testing

#### Test Example
```csharp
[TestFixture]
public class WorkflowExecutionServiceTests
{
    private Mock<IWorkflowRepository> _mockRepository;
    private WorkflowExecutionService _service;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IWorkflowRepository>();
        _service = new WorkflowExecutionService(_mockRepository.Object);
    }

    [Test]
    public async Task ExecuteWorkflowAsync_ValidWorkflow_ShouldExecute()
    {
        // Arrange
        var workflow = new WorkflowDefinition { Id = "test-workflow" };
        _mockRepository.Setup(r => r.GetByIdAsync("test-workflow"))
                      .ReturnsAsync(workflow);

        // Act
        var result = await _service.ExecuteWorkflowAsync("test-workflow");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Running", result.Status);
    }
}
```

### Integration Testing

#### Database Tests
```csharp
[TestFixture]
public class WorkflowRepositoryIntegrationTests
{
    private ElsaDbContext _context;
    private WorkflowRepository _repository;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ElsaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ElsaDbContext(options);
        _repository = new WorkflowRepository(_context);
    }

    [Test]
    public async Task SaveAsync_NewWorkflow_ShouldPersist()
    {
        // Test database operations
    }
}
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Elsa.Shared.Tests/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "TestMethod=ExecuteWorkflowAsync_ValidWorkflow_ShouldExecute"
```

## Debugging

### Visual Studio / VS Code
1. Set breakpoints in code
2. Press F5 to start debugging
3. Use Debug Console for evaluation

### Debug Configuration
```json
// .vscode/launch.json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Launch Studio",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/src/Elsa.Studio.Host/bin/Debug/net8.0/Elsa.Studio.Host.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src/Elsa.Studio.Host",
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            }
        },
        {
            "name": "Launch Runtime",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/src/Elsa.Workflow.Runtime/bin/Debug/net8.0/Elsa.Workflow.Runtime.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src/Elsa.Workflow.Runtime",
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            }
        }
    ]
}
```

### Logging Configuration
```csharp
// Program.cs
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Use Serilog for structured logging
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .WriteTo.Console()
        .WriteTo.File("logs/elsa-.txt", rollingInterval: RollingInterval.Day)
        .MinimumLevel.Debug();
});
```

## Performance Monitoring

### Application Insights
```csharp
// Add Application Insights
services.AddApplicationInsightsTelemetry();

// Custom telemetry
public class WorkflowTelemetryService
{
    private readonly TelemetryClient _telemetryClient;

    public void TrackWorkflowExecution(string workflowId, TimeSpan duration)
    {
        _telemetryClient.TrackMetric("WorkflowExecutionTime", duration.TotalMilliseconds);
        _telemetryClient.TrackEvent("WorkflowExecuted", new Dictionary<string, string>
        {
            ["WorkflowId"] = workflowId
        });
    }
}
```

### Health Checks
```csharp
// Add health checks
services.AddHealthChecks()
    .AddDbContextCheck<ElsaDbContext>()
    .AddNpgSql(connectionString)
    .AddCheck<WorkflowEngineHealthCheck>("workflow-engine");

// Custom health check
public class WorkflowEngineHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Check workflow engine status
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
```

## Best Practices

### Code Quality

#### Coding Standards
- Follow C# coding conventions
- Use meaningful variable and method names
- Write XML documentation for public APIs
- Keep methods small and focused
- Use dependency injection consistently

#### Code Analysis
```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsNotAsErrors>CS1591</WarningsNotAsErrors>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
  </PropertyGroup>
</Project>
```

### Security

#### API Security
```csharp
// Add authentication
services.AddAuthentication("ApiKey")
    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        "ApiKey", options => { });

// Add authorization
services.AddAuthorization(options =>
{
    options.AddPolicy("RequireApiKey", policy =>
        policy.RequireAuthenticatedUser());
});
```

#### Input Validation
```csharp
public class ExecuteWorkflowRequest
{
    [Required]
    [StringLength(100)]
    public string DefinitionId { get; set; }

    [Range(1, int.MaxValue)]
    public int Version { get; set; }

    public Dictionary<string, object> Input { get; set; }
}
```

### Performance

#### Database Optimization
- Use async methods for database operations
- Implement proper indexing
- Use connection pooling
- Monitor query performance

#### Caching Strategy
```csharp
// Add memory caching
services.AddMemoryCache();

// Add distributed caching (Redis)
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// Use caching in services
public class WorkflowDefinitionService
{
    private readonly IMemoryCache _cache;

    public async Task<WorkflowDefinition> GetDefinitionAsync(string id)
    {
        return await _cache.GetOrCreateAsync($"workflow-{id}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _repository.GetByIdAsync(id);
        });
    }
}
```

## Deployment

### Docker Support

#### Dockerfile.Studio
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Elsa.Studio.Host/Elsa.Studio.Host.csproj", "src/Elsa.Studio.Host/"]
COPY ["src/Elsa.Shared/Elsa.Shared.csproj", "src/Elsa.Shared/"]
RUN dotnet restore "src/Elsa.Studio.Host/Elsa.Studio.Host.csproj"
COPY . .
WORKDIR "/src/src/Elsa.Studio.Host"
RUN dotnet build "Elsa.Studio.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Elsa.Studio.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Elsa.Studio.Host.dll"]
```

#### docker-compose.yml
```yaml
version: '3.8'
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: elsa_workflows
      POSTGRES_USER: elsa_user
      POSTGRES_PASSWORD: elsa_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  studio:
    build:
      context: .
      dockerfile: Dockerfile.Studio
    ports:
      - "5001:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=elsa_password
    depends_on:
      - postgres

  runtime:
    build:
      context: .
      dockerfile: Dockerfile.Runtime
    ports:
      - "5002:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=elsa_password
    depends_on:
      - postgres

volumes:
  postgres_data:
```

### CI/CD Pipeline

#### GitHub Actions
```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: postgres
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore
      
    - name: Test
      run: dotnet test --no-build --verbosity normal
```

## Troubleshooting

### Common Issues

#### Database Connection Issues
```bash
# Check PostgreSQL status
docker logs elsa-postgres

# Test connection
psql -h localhost -U elsa_user -d elsa_workflows

# Reset database
docker rm -f elsa-postgres
docker run --name elsa-postgres ...
```

#### Port Conflicts
```bash
# Check port usage
netstat -tulpn | grep :5001

# Kill process using port
kill -9 $(lsof -ti:5001)
```

#### SSL Certificate Issues
```bash
# Trust development certificate
dotnet dev-certs https --trust

# Clear and regenerate
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Logging and Diagnostics

#### Enable Detailed Logging
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information",
      "Elsa": "Debug"
    }
  }
}
```

#### Database Query Logging
```csharp
// Enable EF Core query logging
services.AddDbContext<ElsaDbContext>(options =>
{
    options.UseNpgsql(connectionString)
           .EnableSensitiveDataLogging() // Only in development
           .LogTo(Console.WriteLine, LogLevel.Information);
});
```

This development guide provides comprehensive instructions for setting up, developing, and maintaining the Elsa workflow system. Follow these guidelines to ensure consistent development practices and high-quality code.
