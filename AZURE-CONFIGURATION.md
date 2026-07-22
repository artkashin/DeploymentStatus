# Azure Production Configuration

This file contains template configurations for deploying to Azure Functions in production.

> **⚠️ Critical:** In Azure Portal/environment variables, use **double underscores (`__`)** instead of colons (`:`) for hierarchical configuration. Azure automatically converts `GitHub__Token` to `GitHub:Token` in your .NET code. Use colons only in `local.settings.json` (JSON format).

> **📝 Flex Consumption Plan:** If using Azure Functions **Flex Consumption plan**, do NOT include `FUNCTIONS_WORKER_RUNTIME` in your settings - the runtime is auto-detected.

## Azure Function App Settings

Copy these settings to your Azure Function App Configuration (Configuration → Application settings).

### Required Settings (Consumption/Premium Plans)

```json
{
  "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=<storage-account-name>;AccountKey=<storage-account-key>;EndpointSuffix=core.windows.net",
  "StorageType": "TableStorage",
  "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
  "WEBSITE_RUN_FROM_PACKAGE": "1"
}
```

### Required Settings (Flex Consumption Plan)

```json
{
  "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=<storage-account-name>;AccountKey=<storage-account-key>;EndpointSuffix=core.windows.net",
  "StorageType": "TableStorage",
  "WEBSITE_RUN_FROM_PACKAGE": "1"
}
```

**Note:** Omit `FUNCTIONS_WORKER_RUNTIME` for Flex plan - it auto-detects from your .csproj.

### GitHub Integration - Option A: Personal Access Token

```json
{
  "GitHub__AuthType": "PAT",
  "GitHub__Token": "<github-personal-access-token>",
  "GitHub__Owner": "AdaptiveBS",
  "GitHub__Repository": "CIApp"
}
```

**To create a GitHub PAT:**
1. Go to GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Generate new token (classic)
3. Select scopes: `repo` (Full control of private repositories)
4. Copy the token (you won't see it again!)

### GitHub Integration - Option B: GitHub App (Recommended for Organizations)

```json
{
  "GitHub__AuthType": "GitHubApp",
  "GitHub__AppId": "<github-app-id>",
  "GitHub__InstallationId": "<installation-id>",
  "GitHub__Owner": "AdaptiveBS",
  "GitHub__Repository": "CIApp",
  "GitHub__PrivateKeySource": "KeyVault",
  "GitHub__KeyVaultUrl": "https://<key-vault-name>.vault.azure.net/",
  "GitHub__KeySecretName": "github-app-private-key"
}
```

**Alternative: Store private key directly in app settings (not recommended for production):**
```json
{
  "GitHub__PrivateKeySource": "Configuration",
  "GitHub__PrivateKey": "<base64-encoded-private-key-pem>"
}
```

To base64-encode your private key:
```powershell
# PowerShell
$pemContent = Get-Content -Path "path-to-your-private-key.pem" -Raw
$bytes = [System.Text.Encoding]::UTF8.GetBytes($pemContent)
$base64 = [Convert]::ToBase64String($bytes)
Write-Output $base64
```

### Optional: Application Insights (Recommended)

```json
{
  "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=<instrumentation-key>;IngestionEndpoint=https://<region>.in.applicationinsights.azure.com/;LiveEndpoint=https://<region>.livediagnostics.monitor.azure.com/"
}
```

## local.settings.json Template

Use this template for local development. **Never commit this file with real secrets!**

```json
{
  "IsEncrypted": false,
  "Values": {
	"AzureWebJobsStorage": "UseDevelopmentStorage=true",
	"FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
	"StorageType": "InMemory",

	"GitHub:AuthType": "PAT",
	"GitHub:Token": "ghp_your_github_token_here",
	"GitHub:Owner": "AdaptiveBS",
	"GitHub:Repository": "CIApp"
  }
}
```

## Azure CLI Configuration Commands

### Set all required settings at once:

**Using Personal Access Token:**
```powershell
$RESOURCE_GROUP = "rg-deployment-status"
$FUNCTION_APP = "func-deployment-status-api"
$STORAGE_CONNECTION = "<your-storage-connection-string>"
$GITHUB_TOKEN = "<your-github-pat>"

# Note: Omit FUNCTIONS_WORKER_RUNTIME line if using Flex Consumption plan
az functionapp config appsettings set `
  --name $FUNCTION_APP `
  --resource-group $RESOURCE_GROUP `
  --settings `
	"AzureWebJobsStorage=$STORAGE_CONNECTION" `
	"StorageType=TableStorage" `
	"FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" `
	"GitHub__AuthType=PAT" `
	"GitHub__Token=$GITHUB_TOKEN" `
	"GitHub__Owner=AdaptiveBS" `
	"GitHub__Repository=CIApp"
```

**Using GitHub App:**
```powershell
$RESOURCE_GROUP = "rg-deployment-status"
$FUNCTION_APP = "func-deployment-status-api"
$STORAGE_CONNECTION = "<your-storage-connection-string>"
$GITHUB_APP_ID = "<your-app-id>"
$GITHUB_INSTALLATION_ID = "<your-installation-id>"
$KEY_VAULT_URL = "https://<key-vault-name>.vault.azure.net/"

az functionapp config appsettings set `
  --name $FUNCTION_APP `
  --resource-group $RESOURCE_GROUP `
  --settings `
	"AzureWebJobsStorage=$STORAGE_CONNECTION" `
	"StorageType=TableStorage" `
	"FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" `
	"GitHub__AuthType=GitHubApp" `
	"GitHub__AppId=$GITHUB_APP_ID" `
	"GitHub__InstallationId=$GITHUB_INSTALLATION_ID" `
	"GitHub__Owner=AdaptiveBS" `
	"GitHub__Repository=CIApp" `
	"GitHub__PrivateKeySource=KeyVault" `
	"GitHub__KeyVaultUrl=$KEY_VAULT_URL" `
	"GitHub__KeySecretName=github-app-private-key"
```

## Environment-Specific Settings

### Development (local.settings.json)
```json
{
  "StorageType": "InMemory",
  "AzureWebJobsStorage": "UseDevelopmentStorage=true"
}
```

### Staging
```json
{
  "StorageType": "TableStorage",
  "AzureWebJobsStorage": "<staging-storage-connection-string>",
  "GitHub__Owner": "AdaptiveBS",
  "GitHub__Repository": "CIApp"
}
```

### Production
```json
{
  "StorageType": "TableStorage",
  "AzureWebJobsStorage": "<production-storage-connection-string>",
  "GitHub__Owner": "AdaptiveBS",
  "GitHub__Repository": "CIApp"
}
```

## Using Azure Key Vault (Recommended for Production)

### 1. Store secrets in Key Vault
```powershell
$KEY_VAULT_NAME = "kv-deployment-status"
$GITHUB_TOKEN = "<your-github-pat>"

# Create Key Vault
az keyvault create `
  --name $KEY_VAULT_NAME `
  --resource-group $RESOURCE_GROUP `
  --location eastus

# Store GitHub token
az keyvault secret set `
  --vault-name $KEY_VAULT_NAME `
  --name "github-token" `
  --value $GITHUB_TOKEN

# Store GitHub App private key (if using GitHub App)
az keyvault secret set `
  --vault-name $KEY_VAULT_NAME `
  --name "github-app-private-key" `
  --value "<base64-encoded-pem-key>"
```

### 2. Grant Function App access to Key Vault
```powershell
# Enable managed identity on Function App
az functionapp identity assign `
  --name $FUNCTION_APP `
  --resource-group $RESOURCE_GROUP

# Get the principal ID
$PRINCIPAL_ID = az functionapp identity show `
  --name $FUNCTION_APP `
  --resource-group $RESOURCE_GROUP `
  --query principalId `
  --output tsv

# Grant access to Key Vault
az keyvault set-policy `
  --name $KEY_VAULT_NAME `
  --object-id $PRINCIPAL_ID `
  --secret-permissions get list
```

### 3. Reference secrets in App Settings
```json
{
  "GitHub__Token": "@Microsoft.KeyVault(SecretUri=https://kv-deployment-status.vault.azure.net/secrets/github-token/)",
  "GitHub__PrivateKey": "@Microsoft.KeyVault(SecretUri=https://kv-deployment-status.vault.azure.net/secrets/github-app-private-key/)"
}
```

Or via Azure CLI:
```powershell
az functionapp config appsettings set `
  --name $FUNCTION_APP `
  --resource-group $RESOURCE_GROUP `
  --settings `
	"GitHub__Token=@Microsoft.KeyVault(SecretUri=https://kv-deployment-status.vault.azure.net/secrets/github-token/)"
```

## Using Managed Identity for Storage (Production Best Practice)

Instead of connection strings, use Managed Identity:

### 1. Enable Managed Identity
```powershell
az functionapp identity assign `
  --name $FUNCTION_APP `
  --resource-group $RESOURCE_GROUP
```

### 2. Grant Storage Access
```powershell
$STORAGE_ACCOUNT = "stdeploymentstatus"
$PRINCIPAL_ID = "<function-app-managed-identity-principal-id>"

# Assign Storage Table Data Contributor role
az role assignment create `
  --assignee $PRINCIPAL_ID `
  --role "Storage Table Data Contributor" `
  --scope "/subscriptions/<subscription-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT"
```

### 3. Update App Settings
```json
{
  "AzureWebJobsStorage__accountName": "stdeploymentstatus",
  "StorageType": "TableStorage"
}
```

## GitHub Actions Secrets

Required secrets in your GitHub repository (Settings → Secrets and variables → Actions):

| Secret Name | Description | How to Get |
|-------------|-------------|------------|
| `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` | Azure Function App publish profile | Download from Azure Portal: Function App → Get publish profile |
| `AZURE_STORAGE_CONNECTION_STRING` | Storage account connection string (optional) | Azure Portal: Storage Account → Access keys |

## Verifying Configuration

After setting up configuration, verify it works:

```powershell
# Test the Function App
$FUNCTION_APP = "func-deployment-status-api"

# Get function key
$FUNCTION_KEY = az functionapp keys list `
  --name $FUNCTION_APP `
  --resource-group $RESOURCE_GROUP `
  --query "functionKeys.default" `
  --output tsv

# Test endpoint
curl "https://$FUNCTION_APP.azurewebsites.net/api/update-all-customers/latest?code=$FUNCTION_KEY"
```

## Troubleshooting Configuration Issues

### Issue: "FUNCTIONS_WORKER_RUNTIME is invalid" (Flex Consumption Plan)
- **Solution**: Remove `FUNCTIONS_WORKER_RUNTIME` setting completely
- Flex Consumption plan auto-detects runtime from project file
- Only use this setting for standard Consumption or Premium plans

### Issue: "GitHub authentication failed"
- Verify `GitHub__Token` is set correctly (note the double underscore)
- Check token has not expired
- Ensure token has `repo` scope

### Issue: "Storage connection failed"
- Verify `AzureWebJobsStorage` connection string is valid
- Check storage account is accessible
- Ensure `StorageType` is set to `TableStorage`

### Issue: "Function keys not working"
- Regenerate function keys in Azure Portal
- Check function authorization level in code

### Issue: "Configuration not updating"
- Restart the Function App after changing settings
- Check for typos in setting names (case-sensitive)
- Verify settings are in "Application settings" not "Connection strings"

## Security Checklist

- [ ] Never commit `local.settings.json` with real secrets
- [ ] Use Azure Key Vault for production secrets
- [ ] Enable Managed Identity where possible
- [ ] Rotate secrets regularly
- [ ] Use different credentials for dev/staging/production
- [ ] Enable Application Insights for monitoring
- [ ] Configure IP restrictions for production
- [ ] Use Private Endpoints for enterprise deployments

## Additional Resources

- [Azure Functions Configuration Reference](https://learn.microsoft.com/azure/azure-functions/functions-app-settings)
- [Key Vault References](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references)
- [Managed Identity Guide](https://learn.microsoft.com/azure/app-service/overview-managed-identity)
