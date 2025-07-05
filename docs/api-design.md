# API Design

## Overview
This document describes the REST API design for the Elsa Runtime service, which provides endpoints for managing and executing workflows.

## Base Configuration

### Base URL
- **Development**: `https://localhost:5002/api`
- **Production**: `https://your-domain.com/api`

### API Versioning
- Version header: `API-Version: 1.0`
- URL versioning: `/api/v1/`

### Content Type
- Request: `application/json`
- Response: `application/json`

## Authentication

### API Key Authentication
```http
Authorization: Bearer your-api-key-here
```

### Headers
```http
Content-Type: application/json
API-Version: 1.0
Authorization: Bearer your-api-key-here
```

## Workflow Management API

### 1. Execute Workflow

#### Start Workflow Execution
```http
POST /api/v1/workflows/execute
```

**Request Body:**
```json
{
  "definitionId": "workflow-definition-id",
  "version": 1,
  "correlationId": "optional-correlation-id",
  "input": {
    "parameter1": "value1",
    "parameter2": "value2"
  },
  "contextId": "optional-context-id"
}
```

**Response:**
```json
{
  "workflowInstanceId": "instance-id",
  "status": "Running",
  "correlationId": "correlation-id",
  "createdAt": "2024-01-01T10:00:00Z",
  "output": {}
}
```

#### Execute Workflow Synchronously
```http
POST /api/v1/workflows/execute-sync
```

**Request Body:** (Same as above)

**Response:**
```json
{
  "workflowInstanceId": "instance-id",
  "status": "Completed",
  "correlationId": "correlation-id",
  "createdAt": "2024-01-01T10:00:00Z",
  "finishedAt": "2024-01-01T10:05:00Z",
  "output": {
    "result1": "value1",
    "result2": "value2"
  }
}
```

### 2. Workflow Instance Management

#### Get Workflow Instance
```http
GET /api/v1/workflow-instances/{instanceId}
```

**Response:**
```json
{
  "id": "instance-id",
  "definitionId": "workflow-definition-id",
  "version": 1,
  "status": "Running",
  "subStatus": "Suspended",
  "correlationId": "correlation-id",
  "name": "Workflow Instance Name",
  "createdAt": "2024-01-01T10:00:00Z",
  "updatedAt": "2024-01-01T10:02:00Z",
  "finishedAt": null,
  "faultedAt": null,
  "data": {
    "variables": {
      "var1": "value1"
    }
  },
  "output": {}
}
```

#### List Workflow Instances
```http
GET /api/v1/workflow-instances?page=1&pageSize=20&status=Running
```

**Query Parameters:**
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 20, max: 100)
- `status`: Filter by status (Running, Completed, Faulted, Cancelled)
- `definitionId`: Filter by workflow definition
- `correlationId`: Filter by correlation ID

**Response:**
```json
{
  "items": [
    {
      "id": "instance-id",
      "definitionId": "workflow-definition-id",
      "version": 1,
      "status": "Running",
      "correlationId": "correlation-id",
      "createdAt": "2024-01-01T10:00:00Z"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

#### Cancel Workflow Instance
```http
POST /api/v1/workflow-instances/{instanceId}/cancel
```

**Response:**
```json
{
  "success": true,
  "message": "Workflow instance cancelled successfully"
}
```

#### Resume Workflow Instance
```http
POST /api/v1/workflow-instances/{instanceId}/resume
```

**Request Body:**
```json
{
  "bookmarkId": "bookmark-id",
  "input": {
    "resumeData": "value"
  }
}
```

**Response:**
```json
{
  "success": true,
  "message": "Workflow instance resumed successfully"
}
```

### 3. Workflow Definition Management

#### Get Workflow Definitions
```http
GET /api/v1/workflow-definitions?page=1&pageSize=20
```

**Response:**
```json
{
  "items": [
    {
      "id": "definition-id",
      "definitionId": "logical-definition-id",
      "name": "My Workflow",
      "displayName": "My Workflow Display Name",
      "description": "Workflow description",
      "version": 1,
      "isLatest": true,
      "isPublished": true,
      "createdAt": "2024-01-01T09:00:00Z"
    }
  ],
  "totalCount": 25,
  "page": 1,
  "pageSize": 20,
  "totalPages": 2
}
```

#### Get Workflow Definition
```http
GET /api/v1/workflow-definitions/{definitionId}
```

**Response:**
```json
{
  "id": "definition-id",
  "definitionId": "logical-definition-id",
  "name": "My Workflow",
  "displayName": "My Workflow Display Name",
  "description": "Workflow description",
  "version": 1,
  "isLatest": true,
  "isPublished": true,
  "data": {
    "activities": [...],
    "connections": [...],
    "variables": [...]
  },
  "createdAt": "2024-01-01T09:00:00Z",
  "updatedAt": "2024-01-01T09:30:00Z"
}
```

### 4. Activity Execution Records

#### Get Activity Execution Records
```http
GET /api/v1/workflow-instances/{instanceId}/activity-records
```

**Response:**
```json
{
  "items": [
    {
      "id": "record-id",
      "workflowInstanceId": "instance-id",
      "activityId": "activity-id",
      "activityName": "WriteLine",
      "activityType": "Elsa.Activities.Console.WriteLine",
      "status": "Completed",
      "startedAt": "2024-01-01T10:01:00Z",
      "completedAt": "2024-01-01T10:01:01Z",
      "data": {
        "input": {
          "text": "Hello World"
        },
        "output": {}
      }
    }
  ]
}
```

### 5. Bookmarks Management

#### Get Workflow Bookmarks
```http
GET /api/v1/workflow-instances/{instanceId}/bookmarks
```

**Response:**
```json
{
  "items": [
    {
      "id": "bookmark-id",
      "workflowInstanceId": "instance-id",
      "activityId": "activity-id",
      "name": "WaitForInput",
      "hash": "bookmark-hash",
      "createdAt": "2024-01-01T10:02:00Z",
      "data": {
        "waitingFor": "user-input"
      }
    }
  ]
}
```

#### Trigger Bookmark
```http
POST /api/v1/bookmarks/{bookmarkId}/trigger
```

**Request Body:**
```json
{
  "input": {
    "userInput": "user provided data"
  }
}
```

**Response:**
```json
{
  "success": true,
  "workflowInstanceId": "instance-id",
  "message": "Bookmark triggered successfully"
}
```

## Health and Monitoring API

### 1. Health Check
```http
GET /api/health
```

**Response:**
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "Database",
      "status": "Healthy",
      "duration": "00:00:00.015"
    },
    {
      "name": "WorkflowEngine",
      "status": "Healthy",
      "duration": "00:00:00.001"
    }
  ],
  "totalDuration": "00:00:00.016"
}
```

### 2. Metrics
```http
GET /api/metrics
```

**Response:**
```json
{
  "workflowInstances": {
    "total": 1250,
    "running": 45,
    "completed": 1180,
    "faulted": 25
  },
  "performance": {
    "averageExecutionTime": "00:02:30",
    "throughputPerHour": 120
  },
  "system": {
    "uptime": "2.15:30:45",
    "memoryUsage": "256MB",
    "cpuUsage": "15%"
  }
}
```

## Error Handling

### Error Response Format
```json
{
  "error": {
    "code": "WORKFLOW_NOT_FOUND",
    "message": "Workflow definition not found",
    "details": "The specified workflow definition ID does not exist",
    "timestamp": "2024-01-01T10:00:00Z",
    "traceId": "trace-id-12345"
  }
}
```

### HTTP Status Codes

| Status Code | Description |
|-------------|-------------|
| 200 | OK - Request successful |
| 201 | Created - Resource created successfully |
| 400 | Bad Request - Invalid request data |
| 401 | Unauthorized - Invalid or missing API key |
| 403 | Forbidden - Insufficient permissions |
| 404 | Not Found - Resource not found |
| 409 | Conflict - Resource already exists |
| 422 | Unprocessable Entity - Validation error |
| 500 | Internal Server Error - Server error |
| 503 | Service Unavailable - Service temporarily unavailable |

### Common Error Codes

| Error Code | Description |
|------------|-------------|
| `WORKFLOW_NOT_FOUND` | Workflow definition not found |
| `WORKFLOW_INSTANCE_NOT_FOUND` | Workflow instance not found |
| `INVALID_WORKFLOW_DATA` | Invalid workflow definition data |
| `WORKFLOW_EXECUTION_FAILED` | Workflow execution failed |
| `BOOKMARK_NOT_FOUND` | Bookmark not found |
| `INVALID_API_KEY` | Invalid or expired API key |
| `VALIDATION_ERROR` | Request validation failed |
| `DATABASE_ERROR` | Database operation failed |

## Rate Limiting

### Rate Limits
- **Standard API**: 1000 requests per hour per API key
- **Execution API**: 100 workflow executions per hour per API key
- **Bulk Operations**: 10 requests per minute per API key

### Rate Limit Headers
```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1609459200
```

## Webhooks

### Webhook Events
Configure webhooks to receive notifications about workflow events:

- `workflow.started`
- `workflow.completed`
- `workflow.faulted`
- `workflow.cancelled`
- `activity.completed`
- `activity.faulted`

### Webhook Payload
```json
{
  "eventType": "workflow.completed",
  "timestamp": "2024-01-01T10:05:00Z",
  "workflowInstanceId": "instance-id",
  "definitionId": "workflow-definition-id",
  "correlationId": "correlation-id",
  "data": {
    "status": "Completed",
    "output": {
      "result": "success"
    }
  }
}
```

## SDK Examples

### C# SDK Example
```csharp
var client = new ElsaRuntimeClient("https://localhost:5002", "your-api-key");

// Execute workflow
var request = new ExecuteWorkflowRequest
{
    DefinitionId = "my-workflow",
    Input = new { name = "John", age = 30 }
};

var response = await client.ExecuteWorkflowAsync(request);
Console.WriteLine($"Instance ID: {response.WorkflowInstanceId}");
```

### JavaScript SDK Example
```javascript
const client = new ElsaRuntimeClient({
    baseUrl: 'https://localhost:5002',
    apiKey: 'your-api-key'
});

// Execute workflow
const response = await client.executeWorkflow({
    definitionId: 'my-workflow',
    input: { name: 'John', age: 30 }
});

console.log('Instance ID:', response.workflowInstanceId);
```

## Security Considerations

### API Security
- **HTTPS Only**: All API endpoints must use HTTPS
- **API Key Validation**: Validate API keys on every request
- **Input Validation**: Validate all input parameters
- **Rate Limiting**: Implement rate limiting to prevent abuse
- **Audit Logging**: Log all API requests and responses
- **CORS Configuration**: Configure CORS for web applications

### Data Protection
- **Sensitive Data**: Never log sensitive workflow data
- **Encryption**: Encrypt sensitive data in transit and at rest
- **Access Control**: Implement proper access control for workflows
- **Data Retention**: Implement data retention policies

## Performance Optimization

### Caching Strategy
- Cache workflow definitions for 5 minutes
- Cache frequently accessed workflow instances
- Use Redis for distributed caching in production

### Async Processing
- All workflow executions are asynchronous by default
- Use background services for long-running workflows
- Implement proper timeout handling

### Database Optimization
- Use connection pooling
- Implement database indexes for common queries
- Monitor slow queries and optimize

## Documentation and Testing

### API Documentation
- Swagger/OpenAPI documentation available at `/swagger`
- Interactive API explorer for testing
- Code examples for common operations

### Testing
- Unit tests for all API endpoints
- Integration tests with real workflows
- Load testing for performance validation
- Contract testing for API compatibility
