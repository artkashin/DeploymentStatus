# Azure Deployment Guide

This guide walks you through deploying the DeploymentStatus API to Azure Functions with persistent Azure Table Storage.

## Prerequisites

- Azure subscription
- Visual Studio 2022 or later (for Visual Studio deployment)
- Azure CLI (for command-line deployment)
- Azure Functions Core Tools (optional, for local testing)

## Overview

Your API is ready to deploy! The following are already configured:
- ✅ Azure Functions (.NET 8 isolated worker)
- ✅ Azure Table Storage integration
- ✅ GitHub API integration
- ✅ All workflow customer status endpoints

## Option 1: Deploy via Visual Studio (Recommended)

### Step 1: Create Azure Resources

1. **Create a Resource Group** (if you don't have one):
   - Open [Azure Portal](https://portal.azure.com)
   - Go to "Resource groups" → "Create"
   - Choose your subscription and region
   - Name: `rg-deployment-status`

2. **Create a Storage Account**:
   - In Azure Portal, create a new Storage Account
   - Name: `stdeploymentstatus` (must be globally unique, lowercase, no special chars)
   - Performance: Standard
   - Redundancy: LRS (or your preference)
   - Once created, go to "Access keys" and copy the connection string

3. **Create a Function App**:
   - In Azure Portal, create a new Function App
   - Name: `func-deployment-status-api` (or your choice)
   - **Publish**: Code
   - **Runtime stack**: .NET
   - **Version**: 8 (isolated)
   - **Region**: Same as your resource group
   - **Storage Account**: Select the storage account created above
   - Click "Review + create"

### Step 2: Configure App Settings

After the Function App is created:

1. Go to your Function App in Azure Portal
2. Navigate to **Configuration** → **Application settings**
3. Add/update these settings:

> **⚠️ Important:** In Azure environment variables, use **double underscores (`__`)** instead of colons (`:`) for hierarchical settings. Azure automatically converts `GitHub__Token` to `GitHub:Token` in your .NET configuration.

> **📝 Note for Flex Consumption Plan:** If you're using Azure Functions **Flex Consumption plan**, do NOT add `FUNCTIONS_WORKER_RUNTIME`. The runtime is auto-detected from your project. Only add this setting for Consumption or Premium plans.

| Name | Value | Description |
|------|-------|-------------|
| `AzureWebJobsStorage` | `<storage-connection-string>` | Connection string from your Storage Account |
| `StorageType` | `TableStorage` | Tells the app to use Table Storage |
| `GitHub__Token` | `<your-github-pat>` | GitHub Personal Access Token (if using PAT auth) |
| `GitHub__Owner` | `AdaptiveBS` | GitHub organization/owner name |
| `GitHub__Repository` | `CIApp` | GitHub repository name |
| `GitHub__AuthType` | `PAT` | Authentication type (PAT or GitHubApp) |
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated` | **Consumption/Premium plans only** - omit for Flex plan |

**For GitHub App authentication** (instead of PAT), add:
- `GitHub__AuthType` = `GitHubApp`
- `GitHub__AppId` = `<your-github-app-id>`
- `GitHub__InstallationId` = `<installation-id>`
- `GitHub__PrivateKeySource` = `KeyVault` (or `Configuration`)
- If using KeyVault: `GitHub__KeyVaultUrl`, `GitHub__KeySecretName`
- If using Configuration: `GitHub__PrivateKey` = `<base64-encoded-pem-key>`

4. Click **Save**

### Step 3: Publish from Visual Studio

1. Open the solution in Visual Studio 2022+
2. Right-click on the `DeploymentAPI` project
3. Select **Publish**
4. Choose **Import Profile**
5. Browse to `DeploymentAPI/Properties/PublishProfiles/Azure.pubxml`
6. Update the publish profile with your actual values:
   - `{subscription-id}` → Your Azure subscription ID
   - `{resource-group-name}` → `rg-deployment-status` (or your resource group name)
   - `{function-app-name}` → `func-deployment-status-api` (or your function app name)
7. Click **Publish**

Visual Studio will build and deploy your Function App to Azure!

### Step 4: Verify Deployment

After deployment completes:

1. Go to your Function App in Azure Portal
2. Navigate to **Functions** → You should see all your functions listed
3. Test the API:
   ```bash
   # Get latest update all customers status
   curl https://{function-app-name}.azurewebsites.net/api/update-all-customers/latest?code={function-key}

   # Get specific workflow run customer status
   curl https://{function-app-name}.azurewebsites.net/api/workflow-runs/{runId}/customer-status?code={function-key}
   ```

**To get your function key:**
- Go to Function App → Functions → Select a function → Function Keys → Copy the `default` key

## Option 2: Deploy via Azure CLI

### Prerequisites
```bash
# Install Azure CLI if not already installed
# https://learn.microsoft.com/cli/azure/install-azure-cli

# Login to Azure
az login

# Install Azure Functions Core Tools (if needed)
npm install -g azure-functions-core-tools@4
```

### Deployment Steps

```bash
# Set variables
$RESOURCE_GROUP="rg-deployment-status"
$LOCATION="eastus"
$STORAGE_ACCOUNT="stdeploymentstatus"
$FUNCTION_APP="func-deployment-status-api"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create storage account
az storage account create `
  --name $STORAGE_ACCOUNT `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --sku Standard_LRS

# Get storage connection string
$STORAGE_CONNECTION_STRING = az storage account show-connection-string `
  --resource-group $RESOURCE_GROUP `
  --name $STORAGE_ACCOUNT `
  --query connectionString `
  --output tsv

# Create Function App
az functionapp create `
  --resource-group $RESOURCE_GROUP `
  --consumption-plan-location $LOCATION `
  --runtime dotnet-isolated `
  --runtime-version 8 `
  --functions-version 4 `
  --name $FUNCTION_APP `
  --storage-account $STORAGE_ACCOUNT

# Configure app settings (use double underscores for hierarchical config)
az functionapp config appsettings set `
  --name $FUNCTION_APP `
  --resource-group $RESOURCE_GROUP `
  --settings `
	"StorageType=TableStorage" `
	"GitHub__Owner=AdaptiveBS" `
	"GitHub__Repository=CIApp" `
	"GitHub__Token=<your-github-pat>" `
	"GitHub__AuthType=PAT"

# Build and deploy
cd DeploymentAPI
dotnet publish -c Release -o ./publish
cd publish
Compress-Archive -Path * -DestinationPath ../deploy.zip -Force
cd ..

az functionapp deployment source config-zip `
  --resource-group $RESOURCE_GROUP `
  --name $FUNCTION_APP `
  --src deploy.zip
```

## Continuous Deployment with GitHub Actions

See the GitHub Actions workflow file at `.github/workflows/deploy-azure-functions.yml` for automated CI/CD deployment.

### Setup GitHub Actions Deployment

1. **Get Publish Profile**:
   - Go to your Function App in Azure Portal
   - Click **Get publish profile** (download)
   - Copy the entire XML content

2. **Add GitHub Secret**:
   - Go to your GitHub repository → Settings → Secrets and variables → Actions
   - Click **New repository secret**
   - Name: `AZURE_FUNCTIONAPP_PUBLISH_PROFILE`
   - Value: Paste the publish profile XML
   - Click **Add secret**

3. **Add Storage Connection String Secret** (optional, for additional security):
   - Name: `AZURE_STORAGE_CONNECTION_STRING`
   - Value: Your storage account connection string

4. **Trigger Deployment**:
   - Push to `main` or `develop` branch
   - Or manually trigger from GitHub Actions tab

## API Endpoints

Once deployed, your API endpoints will be available at:

### Deployment Management
- `POST /api/deployments` - Register deployment
- `GET /api/clients/{clientId}/status` - Get client status
- `GET /api/clients/status` - Get all clients status
- `GET /api/deployments/history/{clientId}` - Get deployment history

### CI/CD Version Management
- `GET /api/cicd/version` - Get current CI/CD version
- `POST /api/cicd/version` - Update CI/CD version
- `GET /api/deployments/outdated` - Get outdated deployments

### GitHub Integration
- `GET /api/github/workflows` - Get workflows
- `GET /api/github/workflow-runs` - Get workflow runs
- `GET /api/github/repository` - Get repository info

### **NEW: Workflow Customer Status**
- `GET /api/workflow-runs/{runId}/customer-status` - Get customer installation status for specific workflow run
- `GET /api/update-all-customers/latest` - Get latest "Update all customers" workflow status

## Configuration Reference

### Required App Settings

> **Note:** For Azure Portal/environment variables, use double underscores (`__`). They convert to colons (`:`) in .NET.

| Setting | Example | Description | Required For |
|---------|---------|-------------|-------------|
| `AzureWebJobsStorage` | `DefaultEndpointsProtocol=https;...` | Storage account connection string | All plans |
| `StorageType` | `TableStorage` | Storage provider (TableStorage or InMemory) | All plans |
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated` | Azure Functions runtime | **Consumption & Premium only** (omit for Flex) |

### GitHub Integration Settings

#### Option A: Personal Access Token (Simpler)
| Setting | Example | Description |
|---------|---------|-------------|
| `GitHub__AuthType` | `PAT` | Authentication method |
| `GitHub__Token` | `ghp_xxxxxxxxxxxx` | GitHub Personal Access Token with `repo` scope |
| `GitHub__Owner` | `AdaptiveBS` | GitHub organization/owner name |
| `GitHub__Repository` | `CIApp` | GitHub repository name |

#### Option B: GitHub App (More secure for organizations)
| Setting | Example | Description |
|---------|---------|-------------|
| `GitHub__AuthType` | `GitHubApp` | Authentication method |
| `GitHub__AppId` | `123456` | GitHub App ID |
| `GitHub__InstallationId` | `78901234` | Installation ID for your organization |
| `GitHub__PrivateKeySource` | `KeyVault` | Where to get the private key (KeyVault, Configuration, or File) |
| `GitHub__KeyVaultUrl` | `https://kv-name.vault.azure.net/` | Azure Key Vault URL (if using KeyVault) |
| `GitHub__KeySecretName` | `github-app-private-key` | Secret name in Key Vault (if using KeyVault) |
| `GitHub__Owner` | `AdaptiveBS` | GitHub organization/owner name |
| `GitHub__Repository` | `CIApp` | GitHub repository name |

### Optional Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | - | Application Insights for monitoring (recommended) |

## Testing Your Deployment

### 1. Test Health (No auth needed for GET endpoints with Anonymous level)
```bash
curl https://{function-app-name}.azurewebsites.net/api/clients/status
```

### 2. Test Workflow Customer Status
```bash
# Get latest "Update all customers" status
curl "https://{function-app-name}.azurewebsites.net/api/update-all-customers/latest?code={function-key}"

# Get specific workflow run status
curl "https://{function-app-name}.azurewebsites.net/api/workflow-runs/29418806053/customer-status?code={function-key}"
```

### 3. Register a Test Deployment
```bash
curl -X POST "https://{function-app-name}.azurewebsites.net/api/deployments?code={function-key}" `
  -H "Content-Type: application/json" `
  -d '{
	"clientId": "test-001",
	"clientName": "Test Client",
	"applicationId": "app-test",
	"applicationName": "Test App",
	"version": "1.0.0",
	"status": 0
  }'
```

## Monitoring & Troubleshooting

### Enable Application Insights

1. Create an Application Insights resource in Azure Portal
2. Copy the connection string
3. Add to Function App Configuration:
   - Name: `APPLICATIONINSIGHTS_CONNECTION_STRING`
   - Value: `<your-app-insights-connection-string>`

### View Logs

**In Azure Portal:**
1. Go to your Function App
2. Navigate to **Monitoring** → **Log stream**
3. Watch real-time logs

**Using Azure CLI:**
```bash
az webapp log tail --name $FUNCTION_APP --resource-group $RESOURCE_GROUP
```

### Common Issues

#### Issue: Functions not showing up
- **Solution**: Ensure `FUNCTIONS_WORKER_RUNTIME` is set to `dotnet-isolated`
- Restart the Function App after adding settings

#### Issue: GitHub API calls failing
- **Solution**: Verify GitHub token/app credentials are correct
- Check token has `repo` scope (for PAT)
- Verify GitHub App is installed on the repository

#### Issue: Storage not persisting
- **Solution**: Verify `StorageType` is set to `TableStorage`
- Check `AzureWebJobsStorage` connection string is valid
- Ensure storage account is in the same region

#### Issue: Cold start latency
- **Solution**: Consider using Premium plan or Always On for critical endpoints
- Implement health check warming

## Security Best Practices

1. **Use Managed Identity** (recommended for production):
   - Enable System Assigned Managed Identity on Function App
   - Grant Storage Account access to the identity
   - Remove `AzureWebJobsStorage` connection string from settings

2. **Use Azure Key Vault for secrets**:
   - Store GitHub tokens/keys in Key Vault
   - Reference in App Settings: `@Microsoft.KeyVault(SecretUri=https://...)`

3. **Enable Function Authentication**:
   - Use Function-level (default) or Admin keys
   - Or enable Azure AD authentication

4. **Network Security**:
   - Enable Private Endpoints for production
   - Use VNet integration
   - Configure IP restrictions

## Hosting Plans & Configuration Differences

### Consumption Plan (Standard)
- **Runtime setting:** `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` ✅ Required
- Auto-scaling based on demand
- Cold starts possible
- Best for: Development, variable workloads

### Flex Consumption Plan (Newer)
- **Runtime setting:** Auto-detected from project ❌ Do NOT set
- Fast scaling, reduced cold starts
- Always-ready instances option
- Best for: Modern deployments, better performance
- **Important:** Omit `FUNCTIONS_WORKER_RUNTIME` from configuration

### Premium Plan
- **Runtime setting:** `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` ✅ Required
- Pre-warmed instances
- VNet integration
- No cold starts
- Best for: Production, enterprise

### Dedicated (App Service Plan)
- **Runtime setting:** `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` ✅ Required
- Predictable costs
- Dedicated resources
- Best for: Predictable workloads

## Scaling Considerations

### Consumption Plan (Standard)
- **Pros**: Automatic scaling, pay-per-use
- **Cons**: Cold starts, shared resources
- **Best for**: Development, low-traffic scenarios

### Premium Plan
- **Pros**: Pre-warmed instances, VNet integration, no cold starts
- **Cons**: Higher cost
- **Best for**: Production, high-traffic, enterprise

### Dedicated (App Service Plan)
- **Pros**: Predictable costs, dedicated resources
- **Cons**: Manual scaling configuration
- **Best for**: Predictable workloads

## Cost Estimation

**Consumption Plan (typical usage):**
- Execution: $0.20 per million executions
- Execution time: $0.000016 per GB-second
- Storage: ~$0.10/month for 10GB
- **Estimated**: $5-10/month for moderate traffic

**Premium Plan:**
- ~$150-300/month depending on instance size

## Next Steps

1. ✅ Deploy to Azure using Visual Studio or Azure CLI
2. ✅ Configure Application Insights for monitoring
3. ✅ Set up GitHub Actions for CI/CD (optional)
4. ✅ Test all endpoints
5. ✅ Configure CORS if needed for dashboard access
6. ✅ Update dashboard API endpoint to production URL

## Support & Documentation

- [Azure Functions Documentation](https://learn.microsoft.com/azure/azure-functions/)
- [Azure Table Storage Documentation](https://learn.microsoft.com/azure/storage/tables/)
- [.NET 8 Isolated Worker Guide](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
- Project README: `DeploymentAPI/README.md`
- Workflow API Documentation: `DeploymentAPI/WORKFLOW-CUSTOMER-STATUS-API.md`

## Contact

For issues or questions about this deployment, please refer to:
- GitHub Repository: https://github.com/artkashin/DeploymentStatus
- Project documentation in the repository
