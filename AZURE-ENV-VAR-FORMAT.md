# Azure Environment Variables - Double Underscore Format

## ⚠️ CRITICAL: Environment Variable Naming Convention

When configuring Azure Functions via Azure Portal → Configuration → Application settings, you **MUST** use **double underscores (`__`)** instead of colons (`:`).

## Why This Matters

| Environment | Format | Reason |
|-------------|--------|--------|
| **Azure Portal / Environment Variables** | `GitHub__Token` | Colons not supported in environment variables |
| **local.settings.json** | `"GitHub:Token"` | JSON format, colons work fine |
| **.NET Configuration Code** | `configuration["GitHub:Token"]` | .NET auto-converts `__` to `:` |

## Correct Configuration

### ✅ Azure Portal (Application Settings)

**For Consumption/Premium Plans:**
```
AzureWebJobsStorage    = <connection-string>
StorageType            = TableStorage
FUNCTIONS_WORKER_RUNTIME = dotnet-isolated

GitHub__AuthType       = PAT
GitHub__Token          = ghp_your_token_here
GitHub__Owner          = AdaptiveBS
GitHub__Repository     = CIApp
```

**For Flex Consumption Plan:**
```
AzureWebJobsStorage    = <connection-string>
StorageType            = TableStorage
(DO NOT SET FUNCTIONS_WORKER_RUNTIME - auto-detected)

GitHub__AuthType       = PAT
GitHub__Token          = ghp_your_token_here
GitHub__Owner          = AdaptiveBS
GitHub__Repository     = CIApp
```

### ✅ Azure CLI Commands

```powershell
az functionapp config appsettings set `
  --name func-deployment-status-api `
  --resource-group rg-deployment-status `
  --settings `
	"GitHub__AuthType=PAT" `
	"GitHub__Token=ghp_your_token" `
	"GitHub__Owner=AdaptiveBS" `
	"GitHub__Repository=CIApp"
```

### ✅ local.settings.json (Local Development)

```json
{
  "Values": {
	"GitHub:Token": "ghp_your_token_here",
	"GitHub:Owner": "AdaptiveBS",
	"GitHub:Repository": "CIApp",
	"GitHub:AuthType": "PAT"
  }
}
```

## How .NET Handles This

The .NET configuration system automatically translates:
- Environment variable: `GitHub__Token` → Configuration key: `GitHub:Token`
- Your code: `configuration["GitHub:Token"]` works correctly with either format

## Azure Functions Hosting Plan Differences

| Plan Type | FUNCTIONS_WORKER_RUNTIME Setting | Notes |
|-----------|----------------------------------|-------|
| **Consumption (Standard)** | ✅ Required: `dotnet-isolated` | Traditional serverless plan |
| **Flex Consumption** | ❌ **DO NOT SET** - Auto-detected | Newer plan, better performance |
| **Premium** | ✅ Required: `dotnet-isolated` | Pre-warmed, VNet support |
| **Dedicated (App Service)** | ✅ Required: `dotnet-isolated` | Dedicated resources |

**How to check your plan type:**
- Azure Portal → Function App → Overview → "App Service plan" or "Flex Consumption"

## Common Mistakes

### ❌ WRONG - Using colons in Azure Portal

```
GitHub:Token = ghp_xxx  ← Won't work!
GitHub:Owner = AdaptiveBS  ← Won't work!
```

**Error:** Environment variables with colons will either be ignored or cause parsing errors.

### ❌ WRONG - Using FUNCTIONS_WORKER_RUNTIME on Flex Consumption Plan

```
FUNCTIONS_WORKER_RUNTIME = dotnet-isolated  ← Invalid for Flex plan!
```

**Error:** Flex Consumption plan auto-detects runtime. This setting will cause an error.

### ❌ WRONG - Using double underscores in local.settings.json

```json
{
  "Values": {
	"GitHub__Token": "ghp_xxx"  ← Works, but unconventional
  }
}
```

**Note:** While this technically works, it's confusing. Use colons in JSON files.

## Quick Reference Table

| Setting Name (Azure Portal) | Setting Name (local.settings.json) | Code Reference |
|------------------------------|-------------------------------------|----------------|
| `GitHub__AuthType` | `"GitHub:AuthType"` | `configuration["GitHub:AuthType"]` |
| `GitHub__Token` | `"GitHub:Token"` | `configuration["GitHub:Token"]` |
| `GitHub__Owner` | `"GitHub:Owner"` | `configuration["GitHub:Owner"]` |
| `GitHub__Repository` | `"GitHub:Repository"` | `configuration["GitHub:Repository"]` |
| `GitHub__AppId` | `"GitHub:AppId"` | `configuration["GitHub:AppId"]` |
| `GitHub__InstallationId` | `"GitHub:InstallationId"` | `configuration["GitHub:InstallationId"]` |
| `GitHub__PrivateKeySource` | `"GitHub:PrivateKeySource"` | `configuration["GitHub:PrivateKeySource"]` |
| `GitHub__KeyVaultUrl` | `"GitHub:KeyVaultUrl"` | `configuration["GitHub:KeyVaultUrl"]` |
| `GitHub__KeySecretName` | `"GitHub:KeySecretName"` | `configuration["GitHub:KeySecretName"]` |
| `GitHub__PrivateKey` | `"GitHub:PrivateKey"` | `configuration["GitHub:PrivateKey"]` |

## Verification

After setting configuration in Azure Portal:

1. Go to Function App → Configuration → Application settings
2. Look for settings starting with `GitHub__` (double underscore)
3. Restart the Function App
4. Check logs for successful GitHub authentication

## If You Have Issues

### Symptom: "FUNCTIONS_WORKER_RUNTIME is invalid" error
**Check:**
- Your hosting plan type (Consumption, Flex, Premium, or Dedicated)
- If using **Flex Consumption plan**: Remove `FUNCTIONS_WORKER_RUNTIME` completely
- If using other plans: Ensure it's set to `dotnet-isolated`

### Symptom: GitHub authentication fails
**Check:**
- Settings use `GitHub__Token` not `GitHub:Token`
- Settings are in "Application settings" not "Connection strings"
- Function App has been restarted after changes

### Symptom: Settings not appearing
**Solution:**
- Azure Portal shows underscores correctly
- If you see `GitHub:Token` in portal, you manually typed it - change to `GitHub__Token`

## Updated Documentation Files

All configuration examples have been updated in:
- ✅ `AZURE-DEPLOYMENT.md` - Deployment guide
- ✅ `AZURE-CONFIGURATION.md` - Configuration templates
- ⚠️ Other `*.md` files with GitHub setup still use colons (those are for local.settings.json examples)

## Remember

**Portal/CLI/Environment Variables** → Use `__`  
**local.settings.json** → Use `:`  
**Your C# Code** → Use `:` (reads either format)
