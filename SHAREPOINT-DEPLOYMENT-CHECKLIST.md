# SharePoint Deployment Checklist

## Pre-Deployment

- [ ] API code is working locally (`func start`)
- [ ] Workflow endpoint returns data: `http://localhost:7071/api/update-all-customers/latest`
- [ ] CORS configured in `host.json` for SharePoint origin
- [ ] GitHub App credentials stored in `local.settings.json`

## Deploy API to Azure

### Option 1: Visual Studio
- [ ] Right-click `DeploymentAPI` project
- [ ] Select "Publish"
- [ ] Choose "Azure" → "Azure Functions"
- [ ] Select or create Function App
- [ ] Click "Publish"
- [ ] Wait for deployment to complete

### Option 2: Azure CLI
```powershell
# Login to Azure
az login

# Create resource group (if needed)
az group create --name DeploymentStatusRG --location eastus

# Create Function App
az functionapp create `
	--resource-group DeploymentStatusRG `
	--consumption-plan-location eastus `
	--runtime dotnet-isolated `
	--runtime-version 8 `
	--functions-version 4 `
	--name your-deployment-api `
	--storage-account yourstorageaccount

# Deploy
cd DeploymentAPI
func azure functionapp publish your-deployment-api
```

## Configure Azure Function

- [ ] Go to Azure Portal → Your Function App
- [ ] Navigate to "Configuration" → "Application settings"
- [ ] Add these settings:
  - `GitHubAppId` = your GitHub App ID
  - `GitHubInstallationId` = your installation ID
  - `GitHubPrivateKey` = your private key (PEM format)
  - `GitHubOwner` = AdaptiveBS
  - `GitHubRepo` = CIApp
- [ ] Click "Save"
- [ ] Restart Function App

## Test API in Azure

- [ ] Get your API URL: `https://your-app-name.azurewebsites.net`
- [ ] Test in browser or PowerShell:
  ```powershell
  Invoke-RestMethod -Uri "https://your-app-name.azurewebsites.net/api/update-all-customers/latest"
  ```
- [ ] Verify JSON response with workflow data
- [ ] Check for CORS headers (should include SharePoint origin)

## Prepare Dashboard for SharePoint

### Using Standalone File

- [ ] Open `DeploymentDashboard/dashboard-sharepoint-standalone.html`
- [ ] Find line with `const API_BASE_URL`
- [ ] Replace `'https://your-api.azurewebsites.net/api'` with YOUR Azure Function URL
- [ ] Save file

### Using Separate Files

- [ ] Open `DeploymentDashboard/js/config.js`
- [ ] Update `baseUrl:` to your Azure Function URL
- [ ] Save file

## Upload to SharePoint

- [ ] Go to https://adaptivenav.sharepoint.com/sites/KnowledgeBase
- [ ] Navigate to "Site Contents"
- [ ] Open "Site Assets" (or create "Dashboard" library)
- [ ] Click "Upload" → "Files"
- [ ] Upload dashboard file(s)
- [ ] Verify files uploaded successfully

## Create SharePoint Page

### Option 1: Embed Web Part
- [ ] Click "New" → "Page"
- [ ] Choose "Blank" template
- [ ] Name the page (e.g., "Deployment Dashboard")
- [ ] Add "Embed" web part
- [ ] Click "Add embed code"
- [ ] Paste iframe code:
  ```html
  <iframe src="/sites/KnowledgeBase/SiteAssets/dashboard-sharepoint-standalone.html" 
		  width="100%" 
		  height="1200px" 
		  frameborder="0">
  </iframe>
  ```
- [ ] Resize as needed
- [ ] Click "Publish"

### Option 2: Script Editor Web Part
- [ ] Create classic page or add to existing
- [ ] Insert "Script Editor" web part
- [ ] Click "Edit Snippet"
- [ ] Paste full HTML or iframe code
- [ ] Click "Insert"
- [ ] Save page

## Test Dashboard in SharePoint

- [ ] Open the SharePoint page
- [ ] Check browser console (F12) for errors
- [ ] Verify dashboard loads
- [ ] Confirm CI/CD version displays
- [ ] Verify workflow section shows customer cards
- [ ] Check color coding (green = success, red = failed)
- [ ] Test "Refresh" button
- [ ] Verify data updates

## Troubleshooting Steps

If dashboard doesn't load:

### 1. Check API
- [ ] Open API URL in browser tab
- [ ] Should see Azure Functions page or JSON response
- [ ] Test specific endpoint: `/api/update-all-customers/latest`

### 2. Check CORS
- [ ] Open browser DevTools (F12)
- [ ] Go to Console tab
- [ ] Look for CORS errors (red text)
- [ ] If CORS error: verify `host.json` and redeploy API

### 3. Check SharePoint
- [ ] Verify file uploaded correctly
- [ ] Check file path in iframe/embed is correct
- [ ] Try opening file directly: `https://adaptivenav.sharepoint.com/sites/KnowledgeBase/SiteAssets/dashboard-sharepoint-standalone.html`

### 4. Check Configuration
- [ ] Verify API_BASE_URL in JavaScript
- [ ] Ensure URL ends with `/api` (no trailing slash after)
- [ ] Confirm URL uses `https://` (not `http://`)

## Post-Deployment

- [ ] Share page with team
- [ ] Add to site navigation
- [ ] Set up page permissions if needed
- [ ] Document for team (usage instructions)
- [ ] Monitor API usage in Azure Portal

## Optional Enhancements

- [ ] Add auto-refresh (every 5 minutes)
- [ ] Create mobile-responsive view
- [ ] Add export to Excel functionality
- [ ] Implement user notifications
- [ ] Add historical trending charts

---

## Quick Reference

### Your Configuration
```
Azure Function URL: https://__________.azurewebsites.net/api
SharePoint Site: https://adaptivenav.sharepoint.com/sites/KnowledgeBase
Dashboard File: /SiteAssets/dashboard-sharepoint-standalone.html
```

### Key Endpoints
- Workflow Status: `/api/update-all-customers/latest`
- CI/CD Version: `/api/cicd/version`
- Client Status: `/api/clients/status`

### Support Files
- `SHAREPOINT-DEPLOYMENT-GUIDE.md` - Detailed instructions
- `CORS-FIX-COMPLETE.md` - CORS troubleshooting
- `dashboard-sharepoint-standalone.html` - Ready-to-upload file

---

**Status:**
- [ ] Planning
- [ ] API Deployed
- [ ] Dashboard Configured
- [ ] SharePoint Uploaded
- [ ] Testing
- [ ] ✅ Production Ready!
