# Dashboard Quick Start Guide

## Current Status: ✅ READY

### API Status
- **Running:** ✅ http://localhost:7071
- **Functions Loaded:** ✅ 9/9
- **Workflow Endpoint:** ✅ Working

### What's Working

#### 1. ✅ Workflow Run Status (NEW FEATURE)
- **Endpoint:** `/api/update-all-customers/latest`
- **Data:** Run #17 with 8 customers
- **Success Rate:** 75% (6 successful, 2 failed)
- **Display:** Green cards for success, Red cards for failures

#### 2. ✅ CI/CD Version
- **Endpoint:** `/api/cicd/version`
- **Current Version:** 1.4.0
- **Status:** Working

#### 3. ⚠️ Client Status & History
- **Endpoint:** `/api/clients/status`
- **Status:** Returns empty (expected - in-memory storage)
- **Note:** Data will populate when you register deployments

## How to Use the Dashboard

### Step 1: Ensure API is Running
```powershell
cd DeploymentAPI
func start --port 7071
```

Wait for: "Job host started" message

### Step 2: Open Dashboard
```powershell
start DeploymentDashboard\index.html
```

Or navigate to the HTML file in your browser

### Step 3: Refresh Dashboard
- Press **F5** or **Ctrl+R** in your browser
- Click the **"Refresh"** button in the dashboard header

## Expected Dashboard View

```
┌─────────────────────────────────────────────────────┐
│ Business Central Deployment Dashboard   [Refresh]  │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ CI/CD Version                      [Update Version] │
│ Current Version: 1.4.0                             │
│ Updated: [timestamp]  By: Setup                    │
└─────────────────────────────────────────────────────┘

┌────┬────┬────┬────┐
│ 0  │ 0  │ 0  │ 0  │  ← Will show data when you add clients
│Total│Up  │Behind Apps│
└────┴────┴────┴────┘

┌─────────────────────────────────────────────────────┐
│ Latest Update All Customers Run        [Refresh]   │
├─────────────────────────────────────────────────────┤
│ Update all customers            Run #17             │
│ Run ID: 29418806053  Status: completed             │
│                                                     │
│  [8]       [6]        [2]        [75%]             │
│ Total   ✓ Installed  ✗ Failed  Success Rate       │
├─────────────────────────────────────────────────────┤
│ ┌──────────┐ ┌──────────┐ ┌──────────┐           │
│ │✓ josephs │ │✓baileybox│ │✓ jrdunn  │  ← GREEN  │
│ │  70s     │ │  45s     │ │  50s     │           │
│ └──────────┘ └──────────┘ └──────────┘           │
│ ┌──────────┐ ┌──────────┐ ┌──────────┐           │
│ │✓ eiseman │ │✓ dw      │ │✓ lbgreen │  ← GREEN  │
│ │  46s     │ │  52s     │ │  61s     │           │
│ └──────────┘ └──────────┘ └──────────┘           │
│ ┌──────────┐ ┌──────────┐                        │
│ │✗ bergaro │ │✗ orrs    │            ← RED       │
│ │  28s     │ │  80s     │                        │
│ └──────────┘ └──────────┘                        │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ Client Deployment Status                           │
│ No clients found  ← Expected (in-memory storage)   │
└─────────────────────────────────────────────────────┘
```

## Troubleshooting

### Problem: "Failed to load workflow status"

**Solution 1:** Check if API is running
```powershell
# Test endpoint
Invoke-RestMethod -Uri "http://localhost:7071/api/update-all-customers/latest"
```

**Solution 2:** Restart API
```powershell
# Stop: Ctrl+C in API terminal
# Start: func start --port 7071
```

**Solution 3:** Check browser console (F12)
- Look for CORS errors
- Look for network errors

### Problem: "Failed to load clients"

**This is expected!** The API uses in-memory storage. To add client data:

```powershell
$data = @{
	clientId = "test-client"
	applicationId = "BaseApp"
	version = "1.4.0"
	deployedBy = "Manual Test"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/deployments" `
	-Method Post `
	-Body $data `
	-ContentType "application/json"
```

Then refresh the dashboard.

### Problem: Dashboard shows old data

**Solution:**
1. Hard refresh: **Ctrl+F5** (Windows) or **Cmd+Shift+R** (Mac)
2. Clear browser cache
3. Close and reopen browser

### Problem: CORS errors in console

The API should allow cross-origin requests. If you see CORS errors:

1. Check `host.json` in DeploymentAPI
2. Restart the API
3. Use a local web server instead of opening file directly:
   ```powershell
   cd DeploymentDashboard
   python -m http.server 8000
   # Navigate to: http://localhost:8000
   ```

## Testing Individual Endpoints

### Test Workflow Run Status
```powershell
Invoke-RestMethod -Uri "http://localhost:7071/api/update-all-customers/latest"
```

**Expected:** JSON with 8 customers, 6 successful, 2 failed

### Test Specific Run
```powershell
Invoke-RestMethod -Uri "http://localhost:7071/api/workflow-runs/29418806053/customer-status"
```

### Test CI/CD Version
```powershell
Invoke-RestMethod -Uri "http://localhost:7071/api/cicd/version"
```

**Expected:** `{ "version": "1.4.0", ... }`

### Test Client Status
```powershell
Invoke-RestMethod -Uri "http://localhost:7071/api/clients/status"
```

**Expected:** `{ "clients": [], "ciCdVersion": "1.4.0", ... }`

## Features Summary

### ✅ Fully Working
- Workflow run customer status display
- CI/CD version management
- GitHub Actions integration
- Real-time data from GitHub API

### ⚠️ Limited (In-Memory Storage)
- Client status (resets on API restart)
- Deployment history (resets on API restart)

### 🔄 To Enable Full Features
Use Table Storage instead of in-memory:
1. Configure Azure Storage connection
2. Update `local.settings.json`:
   ```json
   "StorageType": "TableStorage"
   ```
3. Restart API

## Quick Commands Reference

### Start Everything
```powershell
# Terminal 1: Start API
cd DeploymentAPI
func start --port 7071

# Terminal 2: Open Dashboard
start DeploymentDashboard\index.html
```

### Stop API
- Press **Ctrl+C** in the API terminal

### Test All Endpoints
```powershell
# Workflow Status
Invoke-RestMethod "http://localhost:7071/api/update-all-customers/latest"

# CI/CD Version
Invoke-RestMethod "http://localhost:7071/api/cicd/version"

# Client Status
Invoke-RestMethod "http://localhost:7071/api/clients/status"
```

## What's New

### Workflow Run Status Feature 🎉
- **New Section:** "Latest Update All Customers Run"
- **Data Source:** GitHub Actions workflow runs
- **Updates:** Manual refresh or auto-load on page load
- **Display:**
  - Summary statistics (total, successful, failed, success rate)
  - Individual customer cards with color coding
  - Duration and runner information
  - Links to GitHub job details

### API Endpoints Added
- `GET /api/update-all-customers/latest`
- `GET /api/workflow-runs/{runId}/customer-status`

## Support

### Documentation
- `DASHBOARD-INTEGRATION-COMPLETE.md` - Complete integration guide
- `WORKFLOW-STATUS-FEATURE.md` - Feature documentation
- `API-FIXED-AND-RUNNING.md` - Troubleshooting guide
- `CUSTOMER-STATUS-SUMMARY.md` - API summary

### Test Files
- `test-workflow-dashboard.html` - Standalone API tester
- `test-customer-status-api.ps1` - PowerShell test script

---

**Ready to use! Just refresh your browser to see the new workflow status feature! 🎉**
