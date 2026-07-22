# Quick Deployment Commands

## 1. Deploy API to Azure

### First Time Setup
```powershell
# Login to Azure
az login

# Set your subscription (if you have multiple)
az account set --subscription "Your Subscription Name"

# Create resource group
az group create `
	--name DeploymentStatusRG `
	--location eastus

# Create storage account (required for Functions)
$storageAccountName = "deploymentst$(Get-Random -Maximum 9999)"
az storage account create `
	--name $storageAccountName `
	--location eastus `
	--resource-group DeploymentStatusRG `
	--sku Standard_LRS

# Create Function App
$functionAppName = "deployment-status-api-$(Get-Random -Maximum 9999)"
az functionapp create `
	--resource-group DeploymentStatusRG `
	--consumption-plan-location eastus `
	--runtime dotnet-isolated `
	--runtime-version 8 `
	--functions-version 4 `
	--name $functionAppName `
	--storage-account $storageAccountName

Write-Host "`n✅ Function App created!" -ForegroundColor Green
Write-Host "Name: $functionAppName" -ForegroundColor Cyan
Write-Host "URL: https://$functionAppName.azurewebsites.net" -ForegroundColor Cyan
```

### Deploy Code
```powershell
# From DeploymentAPI folder
cd DeploymentAPI

# Publish
func azure functionapp publish $functionAppName

# Or use your function app name directly:
func azure functionapp publish deployment-status-api-1234
```

### Configure App Settings
```powershell
# Set GitHub App credentials
az functionapp config appsettings set `
	--name $functionAppName `
	--resource-group DeploymentStatusRG `
	--settings `
		GitHubAppId="YOUR_APP_ID" `
		GitHubInstallationId="YOUR_INSTALLATION_ID" `
		GitHubOwner="AdaptiveBS" `
		GitHubRepo="CIApp"

# Add private key (multi-line - store in Key Vault recommended)
az functionapp config appsettings set `
	--name $functionAppName `
	--resource-group DeploymentStatusRG `
	--settings GitHubPrivateKey="-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEA...
...your full key here...
-----END RSA PRIVATE KEY-----"

Write-Host "`n✅ App settings configured!" -ForegroundColor Green
```

## 2. Test Deployment

```powershell
# Get your function app URL
$apiUrl = "https://$functionAppName.azurewebsites.net"

# Test workflow endpoint
Write-Host "`nTesting API..." -ForegroundColor Yellow
$result = Invoke-RestMethod -Uri "$apiUrl/api/update-all-customers/latest"

if ($result) {
	Write-Host "✅ API is working!" -ForegroundColor Green
	Write-Host "Run #$($result.runNumber) - $($result.totalCustomers) customers" -ForegroundColor Cyan
	Write-Host "Success: $($result.successfulInstallations) | Failed: $($result.failedInstallations)" -ForegroundColor Cyan
} else {
	Write-Host "❌ API test failed" -ForegroundColor Red
}

# Test CORS
Write-Host "`nTesting CORS..." -ForegroundColor Yellow
$headers = @{
	"Origin" = "https://adaptivenav.sharepoint.com"
}
try {
	$corsTest = Invoke-WebRequest -Uri "$apiUrl/api/update-all-customers/latest" -Headers $headers -Method Options
	Write-Host "✅ CORS configured correctly" -ForegroundColor Green
} catch {
	Write-Host "⚠️  CORS test inconclusive (may still work in browser)" -ForegroundColor Yellow
}

Write-Host "`nYour API URL:" -ForegroundColor Cyan
Write-Host "$apiUrl/api" -ForegroundColor Green
Write-Host "`nUse this URL in dashboard configuration!" -ForegroundColor Yellow
```

## 3. Update Dashboard Configuration

```powershell
# Update standalone file
$standaloneFile = "DeploymentDashboard\dashboard-sharepoint-standalone.html"
$content = Get-Content $standaloneFile -Raw
$content = $content -replace "const API_BASE_URL = '.*?';", "const API_BASE_URL = '$apiUrl/api';"
Set-Content $standaloneFile -Value $content

Write-Host "✅ Updated $standaloneFile with API URL" -ForegroundColor Green

# Update config.js
$configFile = "DeploymentDashboard\js\config.js"
$content = Get-Content $configFile -Raw
$content = $content -replace "baseUrl: '.*?'", "baseUrl: '$apiUrl/api'"
Set-Content $configFile -Value $content

Write-Host "✅ Updated $configFile with API URL" -ForegroundColor Green

Write-Host "`n📄 Files ready for SharePoint upload!" -ForegroundColor Cyan
```

## 4. Quick Status Check

```powershell
# Check if API is running
function Test-DeploymentAPI {
	param(
		[string]$FunctionAppName
	)

	Write-Host "`n🔍 Checking Deployment API Status..." -ForegroundColor Cyan
	Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray

	$apiUrl = "https://$FunctionAppName.azurewebsites.net"

	# Test endpoints
	$endpoints = @(
		@{ Name = "CI/CD Version"; Path = "/api/cicd/version" },
		@{ Name = "Client Status"; Path = "/api/clients/status" },
		@{ Name = "Workflow Status"; Path = "/api/update-all-customers/latest" }
	)

	foreach ($endpoint in $endpoints) {
		try {
			Write-Host "`nTesting: $($endpoint.Name)" -ForegroundColor Yellow
			$result = Invoke-RestMethod -Uri "$apiUrl$($endpoint.Path)" -TimeoutSec 10
			Write-Host "  ✅ $($endpoint.Path)" -ForegroundColor Green
		} catch {
			Write-Host "  ❌ $($endpoint.Path)" -ForegroundColor Red
			Write-Host "     Error: $($_.Exception.Message)" -ForegroundColor DarkRed
		}
	}

	Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
	Write-Host "API URL: $apiUrl/api" -ForegroundColor Cyan
}

# Usage:
# Test-DeploymentAPI -FunctionAppName "your-function-app-name"
```

## 5. Redeploy After Changes

```powershell
# Quick redeploy
cd DeploymentAPI
func azure functionapp publish $functionAppName

# Force rebuild before deploy
dotnet clean
dotnet build --configuration Release
func azure functionapp publish $functionAppName

# View logs after deployment
Write-Host "`nStreaming logs... (Ctrl+C to stop)" -ForegroundColor Yellow
func azure functionapp logstream $functionAppName
```

## 6. Rollback (if needed)

```powershell
# List deployment history
az functionapp deployment list `
	--name $functionAppName `
	--resource-group DeploymentStatusRG

# Rollback to previous deployment
$previousDeploymentId = "DEPLOYMENT_ID_FROM_LIST"
az functionapp deployment source update-token `
	--name $functionAppName `
	--resource-group DeploymentStatusRG `
	--deployment-id $previousDeploymentId
```

## 7. Monitoring

```powershell
# View recent logs
az functionapp log tail `
	--name $functionAppName `
	--resource-group DeploymentStatusRG

# Check app insights (if configured)
az monitor app-insights component show `
	--app $functionAppName `
	--resource-group DeploymentStatusRG

# Get metrics
az monitor metrics list `
	--resource "/subscriptions/YOUR_SUB_ID/resourceGroups/DeploymentStatusRG/providers/Microsoft.Web/sites/$functionAppName" `
	--metric "Requests" `
	--start-time (Get-Date).AddHours(-1).ToString('yyyy-MM-ddTHH:mm:ss') `
	--end-time (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss')
```

## 8. Cleanup (Delete Everything)

```powershell
# WARNING: This deletes everything!
az group delete `
	--name DeploymentStatusRG `
	--yes `
	--no-wait

Write-Host "🗑️  Deletion started (runs in background)" -ForegroundColor Yellow
```

---

## Complete Deployment Script

Save this as `Deploy-ToAzure.ps1`:

```powershell
# Deploy-ToAzure.ps1
param(
	[string]$ResourceGroup = "DeploymentStatusRG",
	[string]$Location = "eastus",
	[string]$GitHubAppId,
	[string]$GitHubInstallationId,
	[string]$GitHubPrivateKeyPath
)

# Validate parameters
if (-not $GitHubAppId -or -not $GitHubInstallationId -or -not $GitHubPrivateKeyPath) {
	Write-Host "❌ Missing required parameters!" -ForegroundColor Red
	Write-Host "Usage: .\Deploy-ToAzure.ps1 -GitHubAppId 123456 -GitHubInstallationId 789012 -GitHubPrivateKeyPath .\private-key.pem"
	exit 1
}

Write-Host "🚀 Starting Azure Deployment..." -ForegroundColor Cyan

# Create resources
$storageAccountName = "deploymentst$(Get-Random -Maximum 9999)"
$functionAppName = "deployment-api-$(Get-Random -Maximum 9999)"

Write-Host "`n1️⃣  Creating resource group..." -ForegroundColor Yellow
az group create --name $ResourceGroup --location $Location | Out-Null

Write-Host "2️⃣  Creating storage account..." -ForegroundColor Yellow
az storage account create `
	--name $storageAccountName `
	--location $Location `
	--resource-group $ResourceGroup `
	--sku Standard_LRS | Out-Null

Write-Host "3️⃣  Creating Function App..." -ForegroundColor Yellow
az functionapp create `
	--resource-group $ResourceGroup `
	--consumption-plan-location $Location `
	--runtime dotnet-isolated `
	--runtime-version 8 `
	--functions-version 4 `
	--name $functionAppName `
	--storage-account $storageAccountName | Out-Null

Write-Host "4️⃣  Configuring app settings..." -ForegroundColor Yellow
$privateKey = Get-Content $GitHubPrivateKeyPath -Raw
az functionapp config appsettings set `
	--name $functionAppName `
	--resource-group $ResourceGroup `
	--settings `
		GitHubAppId=$GitHubAppId `
		GitHubInstallationId=$GitHubInstallationId `
		GitHubOwner="AdaptiveBS" `
		GitHubRepo="CIApp" `
		"GitHubPrivateKey=$privateKey" | Out-Null

Write-Host "5️⃣  Deploying code..." -ForegroundColor Yellow
cd DeploymentAPI
func azure functionapp publish $functionAppName | Out-Null
cd ..

Write-Host "`n✅ Deployment Complete!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "Function App: $functionAppName" -ForegroundColor Cyan
Write-Host "API URL: https://$functionAppName.azurewebsites.net/api" -ForegroundColor Green
Write-Host "Resource Group: $ResourceGroup" -ForegroundColor Cyan
Write-Host "`nTest your API:" -ForegroundColor Yellow
Write-Host "Invoke-RestMethod -Uri 'https://$functionAppName.azurewebsites.net/api/update-all-customers/latest'" -ForegroundColor Gray

# Save configuration
@{
	FunctionAppName = $functionAppName
	ApiUrl = "https://$functionAppName.azurewebsites.net/api"
	ResourceGroup = $ResourceGroup
	DeploymentDate = (Get-Date).ToString()
} | ConvertTo-Json | Set-Content "deployment-config.json"

Write-Host "`n📝 Configuration saved to deployment-config.json" -ForegroundColor Cyan
```

**Usage:**
```powershell
.\Deploy-ToAzure.ps1 `
	-GitHubAppId "YOUR_APP_ID" `
	-GitHubInstallationId "YOUR_INSTALLATION_ID" `
	-GitHubPrivateKeyPath ".\path\to\private-key.pem"
```

---

## Need Help?

- **API not deploying?** Check Azure Function tools are installed: `func --version`
- **Authentication fails?** Verify GitHub App credentials in Azure Portal
- **CORS errors?** Ensure `host.json` has SharePoint origin and redeploy
- **Logs?** Use `func azure functionapp logstream your-app-name`
