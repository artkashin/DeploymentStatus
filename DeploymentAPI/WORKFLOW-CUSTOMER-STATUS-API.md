# Workflow Run Customer Status API

This document describes the new API endpoints for retrieving customer installation status from GitHub Actions workflow runs.

## Overview

These endpoints parse GitHub Actions workflow run jobs to determine which customers had successful installations and which failed during the "Update all customers" workflow.

## Endpoints

### 1. Get Customer Status for Specific Workflow Run

**Endpoint:** `GET /api/workflow-runs/{runId}/customer-status`

**Description:** Gets the installation status for all customers in a specific workflow run.

**Parameters:**
- `runId` (path, required): The GitHub workflow run ID

**Example Request:**
```bash
curl http://localhost:7071/api/workflow-runs/29418806053/customer-status
```

**Example Response:**
```json
{
  "runId": 29418806053,
  "runNumber": 17,
  "workflowName": "Update all customers",
  "status": "completed",
  "overallSuccess": false,
  "totalCustomers": 8,
  "successfulInstallations": 6,
  "failedInstallations": 2,
  "timestamp": "2026-07-15T14:30:00Z",
  "customers": [
	{
	  "name": "josephs",
	  "installed": true,
	  "status": "success",
	  "runner": "CD-joshephs",
	  "durationSeconds": 70,
	  "startedAt": "2026-07-15T13:21:11Z",
	  "completedAt": "2026-07-15T13:22:21Z",
	  "url": "https://github.com/AdaptiveBS/CIApp/actions/runs/29418806053/job/87363703645"
	},
	{
	  "name": "bergaro",
	  "installed": false,
	  "status": "failure",
	  "runner": "BCAPPDEVOPSVM",
	  "durationSeconds": 28,
	  "startedAt": "2026-07-15T13:21:12Z",
	  "completedAt": "2026-07-15T13:21:40Z",
	  "url": "https://github.com/AdaptiveBS/CIApp/actions/runs/29418806053/job/87363707790"
	}
  ]
}
```

### 2. Get Latest "Update all customers" Status

**Endpoint:** `GET /api/update-all-customers/latest`

**Description:** Gets the customer installation status for the most recent "Update all customers" workflow run.

**Example Request:**
```bash
curl http://localhost:7071/api/update-all-customers/latest
```

**Example Response:**
Same format as endpoint #1

## Response Fields

### WorkflowRunCustomerStatusResponse

| Field | Type | Description |
|-------|------|-------------|
| `runId` | number | GitHub workflow run ID |
| `runNumber` | number | Sequential run number |
| `workflowName` | string | Name of the workflow ("Update all customers") |
| `status` | string | Overall workflow status (e.g., "completed") |
| `overallSuccess` | boolean | True if all customers were installed successfully |
| `totalCustomers` | number | Total number of customers in the run |
| `successfulInstallations` | number | Count of successful installations |
| `failedInstallations` | number | Count of failed installations |
| `timestamp` | string | ISO 8601 timestamp when the data was retrieved |
| `customers` | array | Array of customer installation statuses |

### CustomerInstallationStatus

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Customer name |
| `installed` | boolean | True if the update was successfully installed |
| `status` | string | Job conclusion status (success, failure, cancelled) |
| `runner` | string | Name of the runner that executed the job |
| `durationSeconds` | number | Duration of the job in seconds |
| `startedAt` | string | ISO 8601 timestamp when the job started |
| `completedAt` | string | ISO 8601 timestamp when the job completed |
| `url` | string | GitHub URL to the job details |

## Usage Examples

### PowerShell

```powershell
# Get specific run status
$runId = 29418806053
$response = Invoke-RestMethod -Uri "http://localhost:7071/api/workflow-runs/$runId/customer-status"

# Display summary
Write-Host "Total customers: $($response.totalCustomers)"
Write-Host "Successfully installed: $($response.successfulInstallations)"
Write-Host "Failed: $($response.failedInstallations)"

# List failed customers
$response.customers | Where-Object { -not $_.installed } | ForEach-Object {
	Write-Host "Failed: $($_.name) - $($_.status)"
}
```

```powershell
# Get latest run status
$response = Invoke-RestMethod -Uri "http://localhost:7071/api/update-all-customers/latest"

# Display results
$response.customers | Format-Table -Property name, installed, status, runner, durationSeconds
```

### JavaScript/TypeScript

```typescript
// Get specific run status
const runId = 29418806053;
const response = await fetch(`http://localhost:7071/api/workflow-runs/${runId}/customer-status`);
const data = await response.json();

console.log(`Total: ${data.totalCustomers}, Success: ${data.successfulInstallations}, Failed: ${data.failedInstallations}`);

// Filter installed customers
const installed = data.customers.filter(c => c.installed);
console.log('Installed:', installed.map(c => c.name));

// Filter failed customers
const failed = data.customers.filter(c => !c.installed);
console.log('Failed:', failed.map(c => c.name));
```

### C#

```csharp
using System.Net.Http.Json;

var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:7071") };

// Get specific run status
var runId = 29418806053;
var response = await httpClient.GetFromJsonAsync<WorkflowRunCustomerStatusResponse>(
	$"/api/workflow-runs/{runId}/customer-status");

Console.WriteLine($"Total: {response.TotalCustomers}");
Console.WriteLine($"Success: {response.SuccessfulInstallations}");
Console.WriteLine($"Failed: {response.FailedInstallations}");

foreach (var customer in response.Customers.Where(c => !c.Installed))
{
	Console.WriteLine($"Failed: {customer.Name} - {customer.Status}");
}
```

## Error Responses

### 404 Not Found
```json
{
  "error": "Workflow run 12345 not found",
  "message": "Workflow run 12345 not found"
}
```

### 500 Internal Server Error
Returned when there's an error communicating with GitHub or processing the data.

## Implementation Details

### Job Parsing Logic

The API parses GitHub Actions jobs using the following pattern:
- Job name format: `Update {customerName} / Update {customerName}`
- Looks for the "Execute update" step within each job
- A customer is marked as "installed" if the "Execute update" step has a conclusion of "success"

### Authentication

The API uses the same GitHub authentication (GitHub App or Personal Access Token) configured for the DeploymentAPI service.

## Configuration

No additional configuration is required. The endpoints use the existing GitHub configuration:
- `GitHub:Owner`
- `GitHub:Repository`
- GitHub authentication settings

## Testing

### Test the endpoint locally:

1. Start the Azure Functions:
```bash
cd DeploymentAPI
func start
```

2. Test the specific run endpoint:
```bash
curl http://localhost:7071/api/workflow-runs/29418806053/customer-status
```

3. Test the latest run endpoint:
```bash
curl http://localhost:7071/api/update-all-customers/latest
```

## Integration with Dashboard

These endpoints can be integrated into your deployment dashboard to show:
- Real-time customer installation status
- Success/failure metrics
- Installation history
- Customer-specific deployment information

Example dashboard integration:
```javascript
// Fetch and display latest update status
async function updateDashboard() {
	const response = await fetch('/api/update-all-customers/latest');
	const data = await response.json();

	document.getElementById('total-customers').textContent = data.totalCustomers;
	document.getElementById('successful').textContent = data.successfulInstallations;
	document.getElementById('failed').textContent = data.failedInstallations;

	renderCustomerTable(data.customers);
}
```
