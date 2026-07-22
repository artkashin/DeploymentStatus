# Deployment Status API

Business Central deployment tracking system with CI/CD version management and web dashboard.

## Components

### 1. DeploymentAPI (Azure Functions)
REST API for tracking deployments and CI/CD versions.

### 2. DeploymentDashboard (Azure Static Web App)
Web dashboard for visualizing deployment status and history.

## Quick Start

### Start Full Stack (API + Dashboard)
```powershell
.\start-full-stack.ps1
```

This starts:
- Azure Functions API on http://localhost:7071
- Dashboard on http://localhost:8080

### Start Components Separately

**API Only:**
```powershell
.\rebuild-and-start.ps1
```

**Dashboard Only:**
```powershell
.\start-dashboard.ps1
```

### Run Tests
```powershell
# Terminal 1: Start API
.\rebuild-and-start.ps1

# Terminal 2: Run tests
.\test-api.ps1
```

---

## Dashboard Features

- **Real-time Status** - View all client deployments at a glance
- **CI/CD Management** - Update and track CI/CD versions
- **Deployment History** - View detailed deployment logs
- **Version Comparison** - Identify outdated clients
- **Interactive UI** - Click clients to view their history

---

## Available Scripts

| Script | Purpose |
|--------|---------|
| `START.ps1` | Interactive menu with all options |
| `rebuild-and-start.ps1` | Clean build and start API |
| `run-api.ps1` | Build project (helper) |
| `diagnose.ps1` | Check environment and configuration |
| `start-functions.ps1` | Start Azure Functions only |
| `test-api.ps1` | Test all API endpoints |
| `test-version-logic.ps1` | Test version update logic |
| `test-storage-persistence.ps1` | Test data persistence |
| `start-azurite.ps1` | Start Azurite storage emulator |
| `start-with-tablestorage.ps1` | Start with Table Storage |

---

## Storage Options

### In-Memory (Default)
- Fast and simple
- Data lost on restart
- Perfect for development

```json
{
  "StorageType": "InMemory"
}
```

### Azure Table Storage
- Persistent storage
- Data survives restart
- Use Azurite locally

```json
{
  "StorageType": "TableStorage",
  "AzureWebJobsStorage": "UseDevelopmentStorage=true"
}
```

---

## API Endpoints

### CI/CD Version Management
- `POST /api/cicd/version` - Set CI/CD version
- `GET /api/cicd/version` - Get current CI/CD version

### Deployment Tracking
- `POST /api/deployments` - Register deployment
- `GET /api/clients/{clientId}/status` - Get client status
- `GET /api/clients/status` - Get all clients status
- `GET /api/clients/{clientId}/history` - Get deployment history

---

## Architecture

### Two-Table Design

**Deployments (Current State):**
- PartitionKey = ClientId
- RowKey = ApplicationId
- Stores current version only

**DeploymentHistory (Full History):**
- PartitionKey = ClientId
- RowKey = ReversedTimestamp
- Stores all deployments chronologically

### Version Logic

```
Register v1.0.0 (first time):
?? Deployments: INSERT
?? DeploymentHistory: INSERT

Register v1.0.0 (same version):
?? Deployments: SKIP (not updated)
?? DeploymentHistory: INSERT (always added)

Register v1.1.0 (new version):
?? Deployments: UPDATE
?? DeploymentHistory: INSERT
```

---

## Prerequisites

1. **.NET 8 SDK**
   ```powershell
   dotnet --version  # Should be 8.0.x
   ```

2. **Azure Functions Core Tools v4**
   ```powershell
   npm install -g azure-functions-core-tools@4 --unsafe-perm true
   ```

3. **Azurite (optional, for Table Storage)**
   ```powershell
   npm install -g azurite
   ```

---

## Example Usage

```powershell
$baseUrl = "http://localhost:7071/api"

# Set CI/CD version
$cicd = @{ 
    version = "1.3.0"
    updatedBy = "Admin" 
} | ConvertTo-Json
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

---

## Documentation

### Local Development
- **QUICK-START.md** - Quick reference guide
- **CHEATSHEET.md** - Command cheat sheet
- **ARCHITECTURE-STORAGE.md** - Storage architecture details
- **STORAGE-SETUP.md** - Azure Storage setup guide
- **AZURE-STORAGE-INTEGRATION.md** - Table Storage integration

### Dashboard
- **DASHBOARD-PRODUCTION-CONFIG.md** - Production API configuration for dashboard
- **DeploymentDashboard/README.md** - Dashboard setup and deployment

### Azure Deployment
- **AZURE-QUICK-START.md** - **START HERE:** 5-step deployment guide
- **AZURE-DEPLOYMENT.md** - Complete guide to deploying to Azure Functions
- **AZURE-CONFIGURATION.md** - Production configuration templates and secrets management
- **AZURE-ENV-VAR-FORMAT.md** - **IMPORTANT:** Environment variable naming (use `__` not `:`)
- **AZURE-FLEX-PLAN-SETUP.md** - **Flex Consumption Plan:** Omit FUNCTIONS_WORKER_RUNTIME
- **DeploymentAPI/WORKFLOW-CUSTOMER-STATUS-API.md** - Workflow customer status API documentation

---

## Troubleshooting

### API not starting
```powershell
.\diagnose.ps1
```

### Clean rebuild
```powershell
cd DeploymentAPI
dotnet clean
dotnet build
cd ..
```

### View Azurite data
1. Install **Azure Storage Explorer**
2. Connect to `(Emulator - Default Ports)`
3. Browse Tables ? Deployments / DeploymentHistory

---

## Summary

**To start:** Run `.\START.ps1` or `.\rebuild-and-start.ps1`  
**To test:** Run `.\test-api.ps1` (in separate terminal)  
**For help:** Check QUICK-START.md or CHEATSHEET.md  

Built with .NET 8 + Azure Functions + Table Storage
