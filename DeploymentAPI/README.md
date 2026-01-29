# Deployment Status API

Azure Functions API for tracking Business Central deployments with CI/CD version management.

## Features

- Register deployment information for each client
- Track current version status for all client applications
- View deployment status for each application
- Get deployment history for clients
- Compare application versions with CI/CD version
- Monitor current CI/CD version
- Compare client versions with CI/CD and detect outdated deployments

## API Endpoints

### 1. Register Deployment

**POST** `/api/deployments`

Registers a new deployment record.

**Request Body:**
```json
{
  "clientId": "client-001",
  "clientName": "Company ABC",
  "applicationId": "app-hr",
  "applicationName": "HR Module",
  "version": "1.2.5",
  "status": 0
}
```

**Fields:**
- `clientId` (required) - Unique client identifier
- `clientName` (required) - Client name
- `applicationId` (required) - Unique application identifier
- `applicationName` (required) - Application name
- `version` (required) - Application version
- `status` (required) - Deployment status: 0=Success, 1=Failed, 2=InProgress (default: 0)

**Response:**
```json
{
  "message": "Deployment registered successfully",
  "deployment": {
    "clientId": "client-001",
    "applicationId": "app-hr",
    "version": "1.2.5",
    "deploymentTime": "2026-01-27T12:30:00Z",
    "status": 0
  }
}
```

### 2. Get Client Status

**GET** `/api/clients/{clientId}/status`

Returns current deployment status for a specific client.

**Response:**
```json
{
  "clientId": "client-001",
  "clientName": "Company ABC",
  "minVersion": "1.2.5",
  "maxVersion": "1.3.0",
  "ciCdVersion": "1.3.0",
  "applications": [
    {
      "applicationId": "app-hr",
      "applicationName": "HR Module",
      "currentVersion": "1.3.0",
      "lastDeploymentTime": "2026-01-27T12:30:00Z",
      "lastDeploymentStatus": 0
    }
  ]
}
```

### 3. Get All Clients Status

**GET** `/api/clients/status`

Returns deployment status for all clients.

**Response:**
```json
{
  "clients": [
    {
      "clientId": "client-001",
      "clientName": "Company ABC",
      "minVersion": "1.2.5",
      "maxVersion": "1.3.0",
      "ciCdVersion": "1.3.0",
      "applications": [...]
    }
  ],
  "totalClients": 1,
  "generatedAt": "2026-01-27T12:30:00Z"
}
```

### 4. Get Deployment History

**GET** `/api/clients/{clientId}/history?applicationId={applicationId}&limit={limit}`

Returns deployment history for a client.

**Query Parameters:**
- `applicationId` (optional) - Filter by specific application
- `limit` (optional) - Maximum number of records (default: 100)

**Response:**
```json
{
  "clientId": "client-001",
  "count": 5,
  "deployments": [
    {
      "clientId": "client-001",
      "clientName": "Company ABC",
      "applicationId": "app-hr",
      "applicationName": "HR Module",
      "version": "1.3.0",
      "deploymentTime": "2026-01-27T12:30:00Z",
      "status": 0
    }
  ]
}
```

### 5. Set CI/CD Version

**POST** `/api/cicd/version`

Sets the current CI/CD version.

**Request Body:**
```json
{
  "version": "1.3.0",
  "updatedBy": "Admin",
  "notes": "New release with bug fixes"
}
```

**Response:**
```json
{
  "message": "CI/CD version updated successfully",
  "version": {
    "version": "1.3.0",
    "updatedAt": "2026-01-27T12:30:00Z",
    "updatedBy": "Admin",
    "notes": "New release with bug fixes"
  }
}
```

### 6. Get Current CI/CD Version

**GET** `/api/cicd/version`

Returns the current CI/CD version.

**Response:**
```json
{
  "version": "1.3.0",
  "updatedAt": "2026-01-27T12:30:00Z",
  "updatedBy": "Admin",
  "notes": "New release with bug fixes"
}
```

## Storage Architecture

### Two-Table Design

**Deployments Table (Current State):**
- PartitionKey: ClientId
- RowKey: ApplicationId
- Stores only the current version of each application per client
- Updated only when version changes

**DeploymentHistory Table (Full History):**
- PartitionKey: ClientId
- RowKey: ReversedTimestamp (newest first)
- Stores all deployments chronologically
- Always appends new records

### Version Update Logic

```
Register v1.0.0 (first time):
?? Deployments: INSERT/UPDATE
?? DeploymentHistory: INSERT

Register v1.0.0 (same version again):
?? Deployments: SKIP (no update)
?? DeploymentHistory: INSERT (always added)

Register v1.1.0 (new version):
?? Deployments: UPDATE to v1.1.0
?? DeploymentHistory: INSERT
```

## Storage Options

### In-Memory (Default)
```json
{
  "StorageType": "InMemory"
}
```
- Fast and simple
- Data lost on restart
- Perfect for development

### Azure Table Storage
```json
{
  "StorageType": "TableStorage",
  "AzureWebJobsStorage": "UseDevelopmentStorage=true"
}
```
- Persistent storage
- Data survives restart
- Use Azurite locally or Azure Storage in production

## Quick Start

### Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- Azurite (optional, for Table Storage)

### Start Development
```powershell
# From solution root
.\rebuild-and-start.ps1

# Or use interactive menu
.\START.ps1
```

### Run Tests
```powershell
# Terminal 1: Start API
.\rebuild-and-start.ps1

# Terminal 2: Run tests
.\test-api.ps1
```

## Example Usage

```powershell
$baseUrl = "http://localhost:7071/api"

# Set CI/CD version
$cicd = @{ version = "1.3.0"; updatedBy = "Admin" } | ConvertTo-Json
Invoke-RestMethod -Uri "$baseUrl/cicd/version" -Method Post -Body $cicd -ContentType "application/json"

# Register deployment
$dep = @{
    clientId = "client-001"
    clientName = "Test Company"
    applicationId = "app-hr"
    applicationName = "HR Module"
    version = "1.3.0"
    status = 0
} | ConvertTo-Json
Invoke-RestMethod -Uri "$baseUrl/deployments" -Method Post -Body $dep -ContentType "application/json"

# Get client status
Invoke-RestMethod -Uri "$baseUrl/clients/client-001/status"
```

## Project Structure

```
DeploymentAPI/
??? Functions/               # API endpoints
??? Models/                  # Request/Response models
??? Repositories/            # Data access layer
?   ??? Entities/           # Table Storage entities
?   ??? IDeploymentRepository.cs
?   ??? InMemoryDeploymentRepository.cs
?   ??? TableStorageDeploymentRepository.cs
??? Program.cs              # Application entry point
??? host.json               # Functions host configuration
??? local.settings.json     # Local development settings
```

## Documentation

See root folder for additional documentation:
- **README.md** - Main project documentation
- **QUICK-START.md** - Quick reference guide
- **ARCHITECTURE-STORAGE.md** - Detailed storage architecture
- **STORAGE-SETUP.md** - Azure Storage setup guide

