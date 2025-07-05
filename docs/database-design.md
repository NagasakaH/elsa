# Database Design

## Overview
This document describes the database design for the Elsa workflow system, including the core Elsa tables and custom extensions for PostgreSQL.

## Database Schema

### Core Elsa Tables
The following tables are created and managed by Elsa Entity Framework Core:

#### WorkflowDefinitions
Stores workflow definitions and metadata.

| Column | Type | Description |
|--------|------|-------------|
| Id | varchar(255) | Primary key, workflow definition ID |
| DefinitionId | varchar(255) | Logical workflow ID (same across versions) |
| Name | varchar(255) | Workflow name |
| DisplayName | varchar(255) | Human-readable name |
| Description | text | Workflow description |
| Version | int | Version number |
| IsLatest | boolean | Whether this is the latest version |
| IsPublished | boolean | Whether this version is published |
| Data | jsonb | Workflow definition data (activities, connections) |
| CreatedAt | timestamp | Creation timestamp |
| UpdatedAt | timestamp | Last update timestamp |

#### WorkflowInstances
Stores workflow execution instances.

| Column | Type | Description |
|--------|------|-------------|
| Id | varchar(255) | Primary key, instance ID |
| DefinitionId | varchar(255) | Reference to workflow definition |
| DefinitionVersionId | varchar(255) | Reference to specific version |
| Version | int | Workflow version used |
| Status | varchar(50) | Instance status (Running, Completed, Faulted, etc.) |
| SubStatus | varchar(50) | Detailed status |
| CorrelationId | varchar(255) | Correlation ID for tracking |
| Name | varchar(255) | Instance name |
| Data | jsonb | Instance data and variables |
| CreatedAt | timestamp | Creation timestamp |
| UpdatedAt | timestamp | Last update timestamp |
| FinishedAt | timestamp | Completion timestamp |

#### ActivityExecutionRecords
Stores activity execution history.

| Column | Type | Description |
|--------|------|-------------|
| Id | varchar(255) | Primary key |
| WorkflowInstanceId | varchar(255) | Reference to workflow instance |
| ActivityId | varchar(255) | Activity ID within workflow |
| ActivityNodeId | varchar(255) | Node ID in workflow graph |
| ActivityName | varchar(255) | Activity type name |
| ActivityType | varchar(255) | Full activity type |
| Status | varchar(50) | Execution status |
| Data | jsonb | Activity input/output data |
| Exception | text | Exception details if failed |
| StartedAt | timestamp | Execution start time |
| CompletedAt | timestamp | Execution completion time |

#### Bookmarks
Stores workflow bookmarks for resumable activities.

| Column | Type | Description |
|--------|------|-------------|
| Id | varchar(255) | Primary key |
| WorkflowInstanceId | varchar(255) | Reference to workflow instance |
| ActivityId | varchar(255) | Activity ID |
| ActivityNodeId | varchar(255) | Node ID |
| Name | varchar(255) | Bookmark name |
| Hash | varchar(255) | Bookmark hash |
| Data | jsonb | Bookmark data |
| CreatedAt | timestamp | Creation timestamp |

#### Triggers
Stores workflow triggers for automatic execution.

| Column | Type | Description |
|--------|------|-------------|
| Id | varchar(255) | Primary key |
| WorkflowDefinitionId | varchar(255) | Reference to workflow definition |
| WorkflowDefinitionVersionId | varchar(255) | Reference to version |
| ActivityId | varchar(255) | Trigger activity ID |
| Name | varchar(255) | Trigger name |
| Hash | varchar(255) | Trigger hash |
| Data | jsonb | Trigger configuration |

### Custom Extensions

#### WorkflowCategories
Custom table for organizing workflows by category.

| Column | Type | Description |
|--------|------|-------------|
| Id | uuid | Primary key |
| Name | varchar(100) | Category name |
| Description | text | Category description |
| Color | varchar(7) | Hex color code |
| CreatedAt | timestamp | Creation timestamp |
| UpdatedAt | timestamp | Last update timestamp |

#### WorkflowTags
Custom table for tagging workflows.

| Column | Type | Description |
|--------|------|-------------|
| Id | uuid | Primary key |
| WorkflowDefinitionId | varchar(255) | Reference to workflow definition |
| Tag | varchar(50) | Tag name |
| CreatedAt | timestamp | Creation timestamp |

#### CustomActivityDefinitions
Custom table for managing custom activities.

| Column | Type | Description |
|--------|------|-------------|
| Id | uuid | Primary key |
| Name | varchar(100) | Activity name |
| DisplayName | varchar(100) | Display name |
| Description | text | Activity description |
| Category | varchar(50) | Activity category |
| InputProperties | jsonb | Input property definitions |
| OutputProperties | jsonb | Output property definitions |
| AssemblyName | varchar(255) | Assembly containing activity |
| TypeName | varchar(255) | Full type name |
| Version | varchar(20) | Activity version |
| IsEnabled | boolean | Whether activity is enabled |
| CreatedAt | timestamp | Creation timestamp |
| UpdatedAt | timestamp | Last update timestamp |

## Indexes

### Performance Indexes
```sql
-- WorkflowDefinitions indexes
CREATE INDEX idx_workflowdefinitions_definitionid ON WorkflowDefinitions(DefinitionId);
CREATE INDEX idx_workflowdefinitions_islatest ON WorkflowDefinitions(IsLatest);
CREATE INDEX idx_workflowdefinitions_ispublished ON WorkflowDefinitions(IsPublished);

-- WorkflowInstances indexes
CREATE INDEX idx_workflowinstances_definitionid ON WorkflowInstances(DefinitionId);
CREATE INDEX idx_workflowinstances_status ON WorkflowInstances(Status);
CREATE INDEX idx_workflowinstances_correlationid ON WorkflowInstances(CorrelationId);
CREATE INDEX idx_workflowinstances_createdat ON WorkflowInstances(CreatedAt);

-- ActivityExecutionRecords indexes
CREATE INDEX idx_activityexecutionrecords_workflowinstanceid ON ActivityExecutionRecords(WorkflowInstanceId);
CREATE INDEX idx_activityexecutionrecords_startedat ON ActivityExecutionRecords(StartedAt);
CREATE INDEX idx_activityexecutionrecords_status ON ActivityExecutionRecords(Status);

-- Bookmarks indexes
CREATE INDEX idx_bookmarks_workflowinstanceid ON Bookmarks(WorkflowInstanceId);
CREATE INDEX idx_bookmarks_hash ON Bookmarks(Hash);

-- Triggers indexes
CREATE INDEX idx_triggers_workflowdefinitionid ON Triggers(WorkflowDefinitionId);
CREATE INDEX idx_triggers_hash ON Triggers(Hash);
```

### Custom Indexes
```sql
-- Custom table indexes
CREATE INDEX idx_workflowtags_workflowdefinitionid ON WorkflowTags(WorkflowDefinitionId);
CREATE INDEX idx_workflowtags_tag ON WorkflowTags(Tag);
CREATE INDEX idx_customactivitydefinitions_name ON CustomActivityDefinitions(Name);
CREATE INDEX idx_customactivitydefinitions_category ON CustomActivityDefinitions(Category);
```

## Data Relationships

### Entity Relationships
```
WorkflowDefinitions (1) ----< (N) WorkflowInstances
WorkflowInstances (1) ----< (N) ActivityExecutionRecords
WorkflowInstances (1) ----< (N) Bookmarks
WorkflowDefinitions (1) ----< (N) Triggers
WorkflowDefinitions (1) ----< (N) WorkflowTags
WorkflowCategories (1) ----< (N) WorkflowDefinitions (via foreign key)
```

## Database Configuration

### Connection Strings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=elsa_password;Include Error Detail=true",
    "ProductionConnection": "Host=production-db-host;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=complex_password;SSL Mode=Require"
  }
}
```

### Entity Framework Configuration
```csharp
services.AddDbContext<ElsaDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly("Elsa.Shared")));
```

## Migration Strategy

### Database Migrations
1. **Initial Migration**: Create core Elsa tables
2. **Custom Extensions**: Add custom tables and indexes
3. **Version Migrations**: Handle schema changes between versions

### Migration Commands
```bash
# Create migration
dotnet ef migrations add InitialCreate --project Elsa.Shared

# Update database
dotnet ef database update --project Elsa.Shared

# Generate SQL script
dotnet ef migrations script --project Elsa.Shared
```

## Database Switching Strategy

### Repository Pattern
All database access is abstracted through repository interfaces:

```csharp
public interface IWorkflowDefinitionRepository
{
    Task<WorkflowDefinition> GetByIdAsync(string id);
    Task<IEnumerable<WorkflowDefinition>> GetAllAsync();
    Task<WorkflowDefinition> SaveAsync(WorkflowDefinition definition);
    Task DeleteAsync(string id);
}
```

### Database Provider Configuration
```csharp
public static class DatabaseConfiguration
{
    public static void ConfigureDatabase(this IServiceCollection services, 
        string connectionString, DatabaseProvider provider)
    {
        switch (provider)
        {
            case DatabaseProvider.PostgreSQL:
                services.AddDbContext<ElsaDbContext>(options =>
                    options.UseNpgsql(connectionString));
                break;
            case DatabaseProvider.SqlServer:
                services.AddDbContext<ElsaDbContext>(options =>
                    options.UseSqlServer(connectionString));
                break;
            case DatabaseProvider.SQLite:
                services.AddDbContext<ElsaDbContext>(options =>
                    options.UseSqlite(connectionString));
                break;
        }
    }
}
```

## Backup and Recovery

### Backup Strategy
```bash
# Daily backup
pg_dump -h localhost -U elsa_user -d elsa_workflows > elsa_backup_$(date +%Y%m%d).sql

# Compressed backup
pg_dump -h localhost -U elsa_user -d elsa_workflows | gzip > elsa_backup_$(date +%Y%m%d).sql.gz
```

### Recovery Strategy
```bash
# Restore from backup
psql -h localhost -U elsa_user -d elsa_workflows < elsa_backup_20240101.sql
```

## Performance Tuning

### PostgreSQL Configuration
```sql
-- Connection pooling settings
max_connections = 100
shared_buffers = 256MB
effective_cache_size = 1GB
maintenance_work_mem = 64MB
checkpoint_completion_target = 0.9
wal_buffers = 16MB
default_statistics_target = 100
```

### Query Optimization
- Use appropriate indexes for common queries
- Implement connection pooling
- Use async/await patterns
- Implement caching for frequently accessed data
- Monitor slow queries and optimize

## Security Considerations

### Database Security
- Use dedicated database user with minimal permissions
- Enable SSL/TLS for database connections
- Implement database firewall rules
- Regular security updates
- Audit database access
- Encrypt sensitive data in JSONB fields

### Access Control
```sql
-- Create dedicated user
CREATE USER elsa_user WITH PASSWORD 'complex_password';

-- Grant minimal required permissions
GRANT CONNECT ON DATABASE elsa_workflows TO elsa_user;
GRANT USAGE ON SCHEMA public TO elsa_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO elsa_user;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO elsa_user;
