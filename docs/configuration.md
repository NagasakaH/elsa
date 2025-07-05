# Configuration and Deployment

## Overview
This document describes the configuration options and deployment strategies for the Elsa workflow system, including environment settings, database configuration, and production deployment guidelines.

## Configuration Structure

### Configuration Hierarchy
1. **appsettings.json**: Base configuration
2. **appsettings.{Environment}.json**: Environment-specific overrides
3. **Environment Variables**: Runtime overrides
4. **Command Line Arguments**: Highest priority overrides

### Environment Types
- **Development**: Local development environment
- **Staging**: Pre-production testing environment
- **Production**: Live production environment

## Application Configuration

### Elsa Studio Host Configuration

#### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=elsa_password"
  },
  "Elsa": {
    "Studio": {
      "BaseUrl": "https://localhost:5001",
      "ApiKey": "studio-api-key",
      "Features": {
        "WorkflowDesigner": true,
        "ActivityCatalog": true,
        "WorkflowInstances": true,
        "CustomActivities": true
      }
    },
    "Runtime": {
      "BaseUrl": "https://localhost:5002",
      "ApiKey": "runtime-api-key"
    }
  },
  "Authentication": {
    "Schemes": {
      "ApiKey": {
        "ValidApiKeys": {
          "studio-api-key": "Studio",
          "admin-api-key": "Admin"
        }
      }
    }
  },
  "AllowedHosts": "*"
}
```

#### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information",
      "Elsa": "Debug"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=elsa_password;Include Error Detail=true"
  },
  "Elsa": {
    "Studio": {
      "Features": {
        "DeveloperMode": true,
        "DetailedErrors": true
      }
    }
  }
}
```

#### appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Elsa": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=prod-db-server;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=${DB_PASSWORD};SSL Mode=Require"
  },
  "Elsa": {
    "Studio": {
      "BaseUrl": "https://studio.yourdomain.com",
      "Features": {
        "DeveloperMode": false,
        "DetailedErrors": false
      }
    },
    "Runtime": {
      "BaseUrl": "https://runtime.yourdomain.com"
    }
  },
  "AllowedHosts": "studio.yourdomain.com"
}
```

### Elsa Runtime Configuration

#### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=elsa_password"
  },
  "Elsa": {
    "Runtime": {
      "ApiKey": "runtime-api-key",
      "Features": {
        "WorkflowExecution": true,
        "ActivityExecution": true,
        "BookmarkProcessing": true,
        "TriggerProcessing": true
      },
      "Limits": {
        "MaxConcurrentWorkflows": 100,
        "WorkflowTimeoutMinutes": 60,
        "ActivityTimeoutSeconds": 300
      }
    }
  },
  "RateLimiting": {
    "GlobalLimiter": {
      "PermitLimit": 1000,
      "Window": "01:00:00"
    },
    "ExecutionLimiter": {
      "PermitLimit": 100,
      "Window": "01:00:00"
    }
  },
  "HealthChecks": {
    "Enabled": true,
    "Endpoints": {
      "Database": true,
      "WorkflowEngine": true,
      "Memory": true
    }
  },
  "AllowedHosts": "*"
}
```

#### appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Elsa": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=prod-db-server;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=${DB_PASSWORD};SSL Mode=Require"
  },
  "Elsa": {
    "Runtime": {
      "Limits": {
        "MaxConcurrentWorkflows": 500,
        "WorkflowTimeoutMinutes": 30,
        "ActivityTimeoutSeconds": 180
      }
    }
  },
  "RateLimiting": {
    "GlobalLimiter": {
      "PermitLimit": 5000,
      "Window": "01:00:00"
    },
    "ExecutionLimiter": {
      "PermitLimit": 500,
      "Window": "01:00:00"
    }
  },
  "AllowedHosts": "runtime.yourdomain.com"
}
```

## Database Configuration

### Database Provider Configuration

#### PostgreSQL (Default)
```csharp
public static class DatabaseConfiguration
{
    public static void ConfigurePostgreSQL(this IServiceCollection services, 
        string connectionString)
    {
        services.AddDbContext<ElsaDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly("Elsa.Shared");
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
        });
    }
}
```

#### SQL Server
```csharp
public static void ConfigureSqlServer(this IServiceCollection services, 
    string connectionString)
{
    services.AddDbContext<ElsaDbContext>(options =>
    {
        options.UseSqlServer(connectionString, sqlServer =>
        {
            sqlServer.MigrationsAssembly("Elsa.Shared");
            sqlServer.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
    });
}
```

#### SQLite (Development Only)
```csharp
public static void ConfigureSQLite(this IServiceCollection services, 
    string connectionString)
{
    services.AddDbContext<ElsaDbContext>(options =>
    {
        options.UseSqlite(connectionString, sqlite =>
        {
            sqlite.MigrationsAssembly("Elsa.Shared");
        });
    });
}
```

### Database Switching Implementation

#### DatabaseProvider Enum
```csharp
public enum DatabaseProvider
{
    PostgreSQL,
    SqlServer,
    SQLite,
    InMemory
}
```

#### Configuration Extension
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElsaDatabase(this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue<DatabaseProvider>("Database:Provider");
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        switch (provider)
        {
            case DatabaseProvider.PostgreSQL:
                services.ConfigurePostgreSQL(connectionString);
                break;
            case DatabaseProvider.SqlServer:
                services.ConfigureSqlServer(connectionString);
                break;
            case DatabaseProvider.SQLite:
                services.ConfigureSQLite(connectionString);
                break;
            case DatabaseProvider.InMemory:
                services.AddDbContext<ElsaDbContext>(options =>
                    options.UseInMemoryDatabase("ElsaInMemoryDb"));
                break;
            default:
                throw new NotSupportedException($"Database provider {provider} is not supported");
        }

        return services;
    }
}
```

#### Database Configuration Section
```json
{
  "Database": {
    "Provider": "PostgreSQL",
    "Migration": {
      "AutoMigrate": false,
      "SeedData": false
    },
    "Performance": {
      "CommandTimeout": 30,
      "ConnectionPoolSize": 100
    }
  }
}
```

## Environment Variables

### Common Environment Variables
```bash
# Database Configuration
ELSA_DB_PROVIDER=PostgreSQL
ELSA_DB_CONNECTION_STRING="Host=db-server;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=secure_password"

# Application URLs
ELSA_STUDIO_BASE_URL=https://studio.yourdomain.com
ELSA_RUNTIME_BASE_URL=https://runtime.yourdomain.com

# API Keys
ELSA_STUDIO_API_KEY=secure_studio_api_key
ELSA_RUNTIME_API_KEY=secure_runtime_api_key

# Logging
ELSA_LOG_LEVEL=Information
ELSA_LOG_FILE_PATH=/app/logs

# Performance
ELSA_MAX_CONCURRENT_WORKFLOWS=500
ELSA_WORKFLOW_TIMEOUT_MINUTES=30

# Features
ELSA_ENABLE_SWAGGER=false
ELSA_ENABLE_DETAILED_ERRORS=false
ELSA_ENABLE_DEVELOPER_MODE=false
```

### Docker Environment Variables
```bash
# Docker Compose Environment
POSTGRES_DB=elsa_workflows
POSTGRES_USER=elsa_user
POSTGRES_PASSWORD=secure_password

# Application Environment
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80
ASPNETCORE_HTTPS_PORT=443
```

## Security Configuration

### Authentication Configuration

#### API Key Authentication
```csharp
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public Dictionary<string, string> ApiKeys { get; set; } = new();
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string AuthorizationHeaderName = "Authorization";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues) &&
            !Request.Headers.TryGetValue(AuthorizationHeaderName, out var authHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault() ?? 
                           authHeaderValues.FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (Options.ApiKeys.TryGetValue(providedApiKey, out var role))
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, role),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
    }
}
```

#### JWT Authentication (Optional)
```csharp
public static class JwtConfiguration
{
    public static void AddJwtAuthentication(this IServiceCollection services, 
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                };
            });
    }
}
```

### HTTPS Configuration

#### Development Certificates
```bash
# Generate development certificate
dotnet dev-certs https --trust

# Export certificate for Docker
dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p password
```

#### Production SSL Configuration
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:80"
      },
      "Https": {
        "Url": "https://0.0.0.0:443",
        "Certificate": {
          "Path": "/app/certificates/certificate.pfx",
          "Password": "${CERT_PASSWORD}"
        }
      }
    }
  }
}
```

## Docker Deployment

### Docker Compose for Development

#### docker-compose.yml
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: elsa-postgres
    environment:
      POSTGRES_DB: elsa_workflows
      POSTGRES_USER: elsa_user
      POSTGRES_PASSWORD: elsa_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init-db.sql
    networks:
      - elsa-network
    restart: unless-stopped

  studio:
    build:
      context: .
      dockerfile: src/Elsa.Studio.Host/Dockerfile
    container_name: elsa-studio
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_HTTPS_PORT=443
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=elsa_password
    ports:
      - "5001:443"
      - "5011:80"
    volumes:
      - ~/.aspnet/https:/root/.aspnet/https:ro
      - ~/.aspnet/https:/https:ro
    depends_on:
      - postgres
    networks:
      - elsa-network
    restart: unless-stopped

  runtime:
    build:
      context: .
      dockerfile: src/Elsa.Workflow.Runtime/Dockerfile
    container_name: elsa-runtime
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_HTTPS_PORT=443
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=elsa_password
    ports:
      - "5002:443"
      - "5012:80"
    volumes:
      - ~/.aspnet/https:/root/.aspnet/https:ro
      - ~/.aspnet/https:/https:ro
    depends_on:
      - postgres
    networks:
      - elsa-network
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    container_name: elsa-redis
    ports:
      - "6379:6379"
    networks:
      - elsa-network
    restart: unless-stopped

volumes:
  postgres_data:

networks:
  elsa-network:
    driver: bridge
```

### Docker Compose for Production

#### docker-compose.prod.yml
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: elsa-postgres-prod
    environment:
      POSTGRES_DB: elsa_workflows
      POSTGRES_USER: elsa_user
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password
    secrets:
      - db_password
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./backups:/backups
    networks:
      - elsa-network
    restart: always
    deploy:
      resources:
        limits:
          memory: 1GB
          cpus: '0.5'

  studio:
    image: your-registry/elsa-studio:latest
    container_name: elsa-studio-prod
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:443
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=elsa_workflows;Username=elsa_user;Password_File=/run/secrets/db_password
    secrets:
      - db_password
      - ssl_cert
    ports:
      - "443:443"
    networks:
      - elsa-network
    restart: always
    deploy:
      resources:
        limits:
          memory: 512MB
          cpus: '0.5'

  runtime:
    image: your-registry/elsa-runtime:latest
    container_name: elsa-runtime-prod
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:443
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=elsa_workflows;Username=elsa_user;Password_File=/run/secrets/db_password
    secrets:
      - db_password
      - ssl_cert
    ports:
      - "8443:443"
    networks:
      - elsa-network
    restart: always
    deploy:
      replicas: 2
      resources:
        limits:
          memory: 1GB
          cpus: '1.0'

  nginx:
    image: nginx:alpine
    container_name: elsa-nginx
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/ssl:/etc/nginx/ssl:ro
    depends_on:
      - studio
      - runtime
    networks:
      - elsa-network
    restart: always

  redis:
    image: redis:7-alpine
    container_name: elsa-redis-prod
    command: redis-server --requirepass ${REDIS_PASSWORD}
    environment:
      - REDIS_PASSWORD_FILE=/run/secrets/redis_password
    secrets:
      - redis_password
    networks:
      - elsa-network
    restart: always

secrets:
  db_password:
    file: ./secrets/db_password.txt
  redis_password:
    file: ./secrets/redis_password.txt
  ssl_cert:
    file: ./secrets/ssl_cert.pfx

volumes:
  postgres_data:

networks:
  elsa-network:
    driver: bridge
```

### Dockerfiles

#### Elsa Studio Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Elsa.Studio.Host/Elsa.Studio.Host.csproj", "src/Elsa.Studio.Host/"]
COPY ["src/Elsa.Shared/Elsa.Shared.csproj", "src/Elsa.Shared/"]
COPY ["src/Elsa.CustomActivities/Elsa.CustomActivities.csproj", "src/Elsa.CustomActivities/"]

# Restore dependencies
RUN dotnet restore "src/Elsa.Studio.Host/Elsa.Studio.Host.csproj"

# Copy all source code
COPY . .

# Build application
WORKDIR "/src/src/Elsa.Studio.Host"
RUN dotnet build "Elsa.Studio.Host.csproj" -c Release -o /app/build

# Publish application
FROM build AS publish
RUN dotnet publish "Elsa.Studio.Host.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create non-root user
RUN groupadd -r elsa && useradd -r -g elsa elsa
RUN chown -R elsa:elsa /app
USER elsa

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f https://localhost/health || exit 1

ENTRYPOINT ["dotnet", "Elsa.Studio.Host.dll"]
```

#### Elsa Runtime Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Elsa.Workflow.Runtime/Elsa.Workflow.Runtime.csproj", "src/Elsa.Workflow.Runtime/"]
COPY ["src/Elsa.Shared/Elsa.Shared.csproj", "src/Elsa.Shared/"]
COPY ["src/Elsa.CustomActivities/Elsa.CustomActivities.csproj", "src/Elsa.CustomActivities/"]

# Restore dependencies
RUN dotnet restore "src/Elsa.Workflow.Runtime/Elsa.Workflow.Runtime.csproj"

# Copy all source code
COPY . .

# Build application
WORKDIR "/src/src/Elsa.Workflow.Runtime"
RUN dotnet build "Elsa.Workflow.Runtime.csproj" -c Release -o /app/build

# Publish application
FROM build AS publish
RUN dotnet publish "Elsa.Workflow.Runtime.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create non-root user
RUN groupadd -r elsa && useradd -r -g elsa elsa
RUN chown -R elsa:elsa /app
USER elsa

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f https://localhost/health || exit 1

ENTRYPOINT ["dotnet", "Elsa.Workflow.Runtime.dll"]
```

## Kubernetes Deployment

### Kubernetes Manifests

#### Namespace
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: elsa-system
```

#### ConfigMap
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: elsa-config
  namespace: elsa-system
data:
  appsettings.json: |
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
      "Elsa": {
        "Runtime": {
          "Features": {
            "WorkflowExecution": true,
            "ActivityExecution": true
          }
        }
      }
    }
```

#### Secrets
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: elsa-secrets
  namespace: elsa-system
type: Opaque
data:
  db-password: <base64-encoded-password>
  api-key: <base64-encoded-api-key>
```

#### PostgreSQL Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: postgres
  namespace: elsa-system
spec:
  replicas: 1
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
      - name: postgres
        image: postgres:15-alpine
        env:
        - name: POSTGRES_DB
          value: "elsa_workflows"
        - name: POSTGRES_USER
          value: "elsa_user"
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: elsa-secrets
              key: db-password
        ports:
        - containerPort: 5432
        volumeMounts:
        - name: postgres-storage
          mountPath: /var/lib/postgresql/data
      volumes:
      - name: postgres-storage
        persistentVolumeClaim:
          claimName: postgres-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: postgres-service
  namespace: elsa-system
spec:
  selector:
    app: postgres
  ports:
  - port: 5432
    targetPort: 5432
```

#### Elsa Runtime Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: elsa-runtime
  namespace: elsa-system
spec:
  replicas: 3
  selector:
    matchLabels:
      app: elsa-runtime
  template:
    metadata:
      labels:
        app: elsa-runtime
    spec:
      containers:
      - name: elsa-runtime
        image: your-registry/elsa-runtime:latest
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          value: "Host=postgres-service;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=$(DB_PASSWORD)"
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: elsa-secrets
              key: db-password
        ports:
        - containerPort: 80
        - containerPort: 443
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
        volumeMounts:
        - name: config-volume
          mountPath: /app/appsettings.json
          subPath: appsettings.json
      volumes:
      - name: config-volume
        configMap:
          name: elsa-config
---
apiVersion: v1
kind: Service
metadata:
  name: elsa-runtime-service
  namespace: elsa-system
spec:
  selector:
    app: elsa-runtime
  ports:
  - name: http
    port: 80
    targetPort: 80
  - name: https
    port: 443
    targetPort: 443
  type: ClusterIP
```

#### Ingress
```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: elsa-ingress
  namespace: elsa-system
  annotations:
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/force-ssl-redirect: "true"
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
spec:
  tls:
  - hosts:
    - studio.yourdomain.com
    - runtime.yourdomain.com
    secretName: elsa-tls
  rules:
  - host: studio.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: elsa-studio-service
            port:
              number: 80
  - host: runtime.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: elsa-runtime-service
            port:
              number: 80
```

## Monitoring and Observability

### Application Insights Configuration
```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key;IngestionEndpoint=https://your-region.in.applicationinsights.azure.com/"
  },
  "Logging": {
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft": "Warning"
      }
    }
  }
}
```

### Prometheus Metrics
```csharp
public static class MetricsConfiguration
{
    public static void AddPrometheusMetrics(this IServiceCollection services)
    {
        services.AddSingleton<IMetricsRegistry, MetricsRegistry>();
        services.AddSingleton<WorkflowMetrics>();
    }
}

public class WorkflowMetrics
{
    private readonly Counter _workflowExecutions;
    private readonly Histogram _workflowDuration;
    private readonly Gauge _activeWorkflows;

    public WorkflowMetrics()
    {
        _workflowExecutions = Metrics.CreateCounter(
            "elsa_workflow_executions_total",
            "Total number of workflow executions",
            new[] { "status", "workflow_type" });

        _workflowDuration = Metrics.CreateHistogram(
            "elsa_workflow_duration_seconds",
            "Workflow execution duration",
            new[] { "workflow_type" });

        _activeWorkflows = Metrics.CreateGauge(
            "elsa_active_workflows",
            "Number of currently active workflows");
    }

    public void RecordWorkflowExecution(string status, string workflowType)
    {
        _workflowExecutions.WithLabels(status, workflowType).Inc();
    }

    public void RecordWorkflowDuration(double durationSeconds, string workflowType)
    {
        _workflowDuration.WithLabels(workflowType).Observe(durationSeconds);
    }

    public void SetActiveWorkflows(int count)
    {
        _activeWorkflows.Set(count);
    }
}
```

### Logging Configuration

#### Serilog Configuration
```csharp
public static class LoggingConfiguration
{
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "Elsa")
                .WriteTo.Console(outputTemplate: 
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/elsa-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: 
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}");

            if (context.HostingEnvironment.IsProduction())
            {
                configuration.WriteTo.ApplicationInsights(
                    services.GetService<TelemetryConfiguration>(),
                    TelemetryConverter.Traces);
            }
        });
    }
}
```

## Performance Tuning

### Database Connection Pooling
```csharp
public static class PerformanceConfiguration
{
    public static void ConfigurePerformance(this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Configure EF Core performance
        services.AddDbContextPool<ElsaDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsql =>
            {
                npgsql.CommandTimeout(30);
                npgsql.EnableRetryOnFailure(3);
            });
        }, poolSize: 128);

        // Configure memory cache
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1000;
            options.CompactionPercentage = 0.2;
        });

        // Configure distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "Elsa";
        });

        // Configure HTTP client factory
        services.AddHttpClient("default", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            MaxConnectionsPerServer = 100
        });
    }
}
```

### Rate Limiting Configuration
```csharp
public static class RateLimitingConfiguration
{
    public static void ConfigureRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 1000,
                        Window = TimeSpan.FromHours(1)
                    }));

            options.AddPolicy("ExecutionApi", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromHours(1)
                    }));
        });
    }
}
```

## Backup and Recovery

### Database Backup Script
```bash
#!/bin/bash

# Database backup script
DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="elsa_workflows"
DB_USER="elsa_user"
BACKUP_DIR="/backups"
DATE=$(date +%Y%m%d_%H%M%S)

# Create backup
pg_dump -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME \
  --verbose --clean --no-owner --no-privileges \
  > $BACKUP_DIR/elsa_backup_$DATE.sql

# Compress backup
gzip $BACKUP_DIR/elsa_backup_$DATE.sql

# Remove backups older than 30 days
find $BACKUP_DIR -name "elsa_backup_*.sql.gz" -mtime +30 -delete

echo "Backup completed: $BACKUP_DIR/elsa_backup_$DATE.sql.gz"
```

### Recovery Script
```bash
#!/bin/bash

# Database recovery script
BACKUP_FILE=$1
DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="elsa_workflows"
DB_USER="elsa_user"

if [ -z "$BACKUP_FILE" ]; then
    echo "Usage: $0 <backup_file>"
    exit 1
fi

# Stop applications
docker-compose stop studio runtime

# Restore database
if [[ $BACKUP_FILE == *.gz ]]; then
    gunzip -c $BACKUP_FILE | psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME
else
    psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME < $BACKUP_FILE
fi

# Start applications
docker-compose start studio runtime

echo "Recovery completed from: $BACKUP_FILE"
```

This configuration guide provides comprehensive instructions for configuring and deploying the Elsa workflow system across different environments, from development to production, with security, monitoring, and performance considerations.
