# Quick Start Guide

## How to Run

### 1. Start the API
```powershell
cd DeploymentAPI
func start --port 7071 --cors '*'
```

### 2. Open Dashboard
```powershell
start DeploymentDashboard\index.html
```

### 3. To Populate Data from GitHub

Edit `DeploymentAPI\local.settings.json`:
```json
{
  "Values": {
	"GitHub:AuthType": "PAT",
	"GitHub:Token": "ghp_YOUR_TOKEN_HERE"
  }
}
```

Generate token at: https://github.com/settings/tokens/new
Scopes needed: repo, workflow

Then restart API and click "Sync from GitHub" in dashboard.

## API Endpoints
- GET /api/clients/status - All customers with apps
- GET /api/customers - Customer list
- GET /api/applications - Application list
- POST /api/sync/workflow-data - Sync from GitHub

## Current Status
✅ API started (check PowerShell window)
✅ Dashboard opened in browser
⚠️ Database empty (click Sync after fixing GitHub auth)
