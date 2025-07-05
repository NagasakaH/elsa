# Elsa Workflow System Architecture

## Overview
This document describes the architecture of the Elsa workflow system, consisting of two main applications: Elsa Studio (workflow designer) and Elsa Runtime (workflow execution engine).

## System Architecture

### High-Level Components

```
┌─────────────────────────────────────────────────────────────────┐
│                    Elsa Workflow System                         │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────┐    ┌─────────────────────────────────┐ │
│  │   Elsa Studio       │    │     Elsa Runtime                │ │
│  │   (Designer)        │    │     (Execution Engine)         │ │
│  │                     │    │                                 │ │
│  │ - Workflow Design   │    │ - Workflow Execution            │ │
│  │ - Visual Editor     │    │ - HTTP API                      │ │
│  │ - Management UI     │    │ - Activity Processing           │ │
│  │ - Version Control   │    │ - Instance Management           │ │
│  └─────────────────────┘    └─────────────────────────────────┘ │
│           │                                   │                  │
│           └─────────────┬─────────────────────┘                  │
│                         │                                        │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │              Elsa Shared Library                            │ │
│  │                                                             │ │
│  │ - Database Abstraction Layer                                │ │
│  │ - Repository Pattern                                        │ │
│  │ - Common Utilities                                          │ │
│  │ - Configuration Management                                  │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                         │                                        │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │           Elsa Custom Activities                            │ │
│  │                                                             │ │
│  │ - Base Activity Classes                                     │ │
│  │ - Activity Registration                                     │ │
│  │ - Custom Business Logic                                     │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                          │
    ┌─────────────────────────────────────────────────────────────┐
    │                   PostgreSQL Database                       │
    │                                                             │
    │ - Workflow Definitions                                      │
    │ - Workflow Instances                                        │
    │ - Execution History                                         │
    │ - Activity Data                                             │
    └─────────────────────────────────────────────────────────────┘
```

### Component Details

#### 1. Elsa Studio Host
- **Purpose**: Workflow design and management interface
- **Technology**: ASP.NET Core Web Application (.NET 8)
- **Port**: https://localhost:5001
- **Features**:
  - Visual workflow designer
  - Workflow versioning
  - Activity catalog management
  - Workflow testing and debugging
  - User management and permissions

#### 2. Elsa Runtime
- **Purpose**: Workflow execution engine
- **Technology**: ASP.NET Core Web API (.NET 8)
- **Port**: https://localhost:5002
- **Features**:
  - Workflow execution engine
  - REST API for workflow management
  - Activity execution
  - Instance tracking and monitoring
  - Event-driven processing

#### 3. Elsa Shared Library
- **Purpose**: Common functionality shared between Studio and Runtime
- **Technology**: .NET 8 Class Library
- **Features**:
  - Database abstraction layer
  - Repository pattern implementation
  - Entity Framework Core models
  - Common utilities and extensions
  - Configuration management

#### 4. Elsa Custom Activities
- **Purpose**: Custom business logic activities
- **Technology**: .NET 8 Class Library
- **Features**:
  - Base activity classes
  - Activity registration system
  - Input/output property definitions
  - Custom business logic implementation

### Technology Stack

#### Backend
- **.NET 8**: Primary framework
- **ASP.NET Core**: Web framework
- **Entity Framework Core**: ORM
- **PostgreSQL**: Primary database
- **Elsa Workflows v3**: Workflow engine

#### Frontend
- **Blazor Server**: For Studio UI
- **Bootstrap**: UI framework
- **JavaScript**: Client-side interactivity

#### Database
- **PostgreSQL**: Primary database
- **Entity Framework Core**: Database access
- **Repository Pattern**: Data access abstraction

#### Development Tools
- **Docker**: PostgreSQL development environment
- **Visual Studio/VS Code**: Development IDE
- **Git**: Version control

### Data Flow

#### Workflow Design Flow
1. User accesses Elsa Studio
2. Designs workflow using visual editor
3. Workflow definition saved to PostgreSQL
4. Workflow published and versioned

#### Workflow Execution Flow
1. External system calls Runtime API
2. Runtime loads workflow definition from database
3. Workflow engine executes activities
4. Results and state saved to database
5. Completion status returned to caller

### Security Considerations
- HTTPS enforcement
- API key authentication for Runtime
- Role-based access control in Studio
- Database connection security
- Input validation and sanitization

### Performance Considerations
- Connection pooling for database access
- Async/await patterns throughout
- Caching for frequently accessed data
- Horizontal scaling capability
- Background job processing

### Scalability
- Stateless application design
- Database connection pooling
- Horizontal scaling support
- Background job processing
- Event-driven architecture

### Monitoring and Logging
- Structured logging with Serilog
- Application metrics collection
- Health checks for both applications
- Database performance monitoring
- Error tracking and alerting
