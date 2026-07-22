# Azure Functions Flex Consumption Plan - Quick Setup

## ⚡ For Flex Consumption Plan Users

If you're using the newer **Azure Functions Flex Consumption plan**, follow these specific instructions.

## Key Difference: No FUNCTIONS_WORKER_RUNTIME

❌ **DO NOT SET:** `FUNCTIONS_WORKER_RUNTIME`  
✅ **AUTO-DETECTED:** Runtime is determined from your .csproj file

## Correct Configuration for Flex Plan

### Azure Portal → Configuration → Application Settings

```
AzureWebJobsStorage       = DefaultEndpointsProtocol=https;AccountName=xxx;...
StorageType               = TableStorage

GitHub__AuthType          = PAT
GitHub__Token             = ghp_your_github_token_here
GitHub__Owner             = AdaptiveBS
GitHub__Repository        = CIApp
```

**DO NOT ADD:**
- ~~`FUNCTIONS_WORKER_RUNTIME`~~ ← Will cause "invalid" error

## Azure CLI Command for Flex Plan

```powershell
$RESOURCE_GROUP = "rg-deployment-status"
$FUNCTION_APP = "func-deployment-status-api"
$STORAGE_CONNECTION = "<your-storage-connection-string>"
$GITHUB_TOKEN = "<your-github-pat>"

az functionapp config appsettings set `
  --name $FUNCTION_APP `
  --resource-group $RESOURCE_GROUP `
  --settings `
	"AzureWebJobsStorage=$STORAGE_CONNECTION" `
	"StorageType=TableStorage" `
	"GitHub__AuthType=PAT" `
	"GitHub__Token=$GITHUB_TOKEN" `
	"GitHub__Owner=AdaptiveBS" `
	"GitHub__Repository=CIApp"
```

**Note:** No `FUNCTIONS_WORKER_RUNTIME` in the command!

## Creating a Flex Consumption Function App

### Via Azure Portal

1. Create Function App
2. **Hosting:** Choose **Flex Consumption**
3. Runtime: .NET
4. Version: 8 (isolated)
5. Storage: Select your storage account
6. Click Create

### Via Azure CLI

```powershell
az functionapp create `
  --resource-group $RESOURCE_GROUP `
  --name $FUNCTION_APP `
  --storage-account $STORAGE_ACCOUNT `
  --runtime dotnet-isolated `
  --runtime-version 8 `
  --functions-version 4 `
  --flexconsumption-location $LOCATION
```

**Note:** Use `--flexconsumption-location` instead of `--consumption-plan-location`

## How to Check Your Plan Type

**Azure Portal:**
1. Go to your Function App
2. Click Overview
3. Look at "App Service plan" section
   - If it says **"Flex Consumption"** → Use Flex config (no FUNCTIONS_WORKER_RUNTIME)
   - If it says **"Consumption"** or plan name → Use standard config (add FUNCTIONS_WORKER_RUNTIME)

## Comparison: Flex vs Standard Consumption

| Feature | Standard Consumption | Flex Consumption |
|---------|---------------------|------------------|
| Cold Start | Moderate | Reduced |
| Scaling | Auto | Faster auto-scaling |
| Always-Ready Instances | No | Optional |
| Configuration Complexity | Higher | Lower (auto-detects runtime) |
| `FUNCTIONS_WORKER_RUNTIME` | ✅ Required | ❌ Omit (auto-detected) |
| VNet Support | Limited | Better support |
| Cost | Pay per execution | Pay per execution + optional always-ready |

## Benefits of Flex Consumption

✅ **Faster cold starts** - Improved performance  
✅ **Auto-runtime detection** - Less configuration  
✅ **Better scaling** - Handles traffic spikes better  
✅ **Always-ready option** - Can eliminate cold starts entirely  
✅ **Modern architecture** - Designed for .NET 8 isolated worker  

## Migrating from Standard to Flex

If you have an existing Function App on standard Consumption plan:

### Option 1: Update Existing Function App (if supported)
1. Check if your Function App supports in-place migration
2. Remove `FUNCTIONS_WORKER_RUNTIME` setting
3. Restart Function App

### Option 2: Create New Function App (recommended)
1. Create new Function App with Flex Consumption plan
2. Configure settings (without `FUNCTIONS_WORKER_RUNTIME`)
3. Deploy your code
4. Test thoroughly
5. Update DNS/endpoints
6. Delete old Function App

## Complete Minimal Configuration

**All you need for Flex Consumption:**

```
Storage:
  AzureWebJobsStorage = <your-storage-connection-string>
  StorageType = TableStorage

GitHub:
  GitHub__AuthType = PAT
  GitHub__Token = ghp_xxxxxxxxxxxx
  GitHub__Owner = AdaptiveBS
  GitHub__Repository = CIApp

Optional:
  APPLICATIONINSIGHTS_CONNECTION_STRING = <your-app-insights>
```

**That's it!** No `FUNCTIONS_WORKER_RUNTIME` needed.

## Troubleshooting Flex Plan

### Error: "FUNCTIONS_WORKER_RUNTIME is invalid"
**Solution:**
1. Go to Configuration → Application settings
2. Find `FUNCTIONS_WORKER_RUNTIME`
3. Click Delete
4. Click Save
5. Restart Function App

### Functions not loading
**Check:**
- Verify your .csproj has correct target framework: `<TargetFramework>net8.0</TargetFramework>`
- Ensure `<OutputType>Exe</OutputType>` is set
- Confirm `<AzureFunctionsVersion>v4</AzureFunctionsVersion>`

### Deployment failing
**Verify:**
- Using correct publish profile
- Function App is Flex Consumption plan
- No `FUNCTIONS_WORKER_RUNTIME` in configuration
- Storage account is accessible

## Visual Studio Publish Profile for Flex

Your `Azure.pubxml` works the same way - Visual Studio handles the differences automatically.

## Testing Your Flex Function App

```powershell
# Get function key
$FUNCTION_KEY = az functionapp keys list `
  --name $FUNCTION_APP `
  --resource-group $RESOURCE_GROUP `
  --query "functionKeys.default" `
  --output tsv

# Test endpoint
curl "https://$FUNCTION_APP.azurewebsites.net/api/update-all-customers/latest?code=$FUNCTION_KEY"
```

## Documentation References

- Main deployment guide: `AZURE-DEPLOYMENT.md`
- Configuration templates: `AZURE-CONFIGURATION.md`
- Environment variable format: `AZURE-ENV-VAR-FORMAT.md`
- [Microsoft Docs: Flex Consumption Plan](https://learn.microsoft.com/azure/azure-functions/flex-consumption-plan)

## Quick Checklist

- [ ] Using Flex Consumption plan
- [ ] Removed `FUNCTIONS_WORKER_RUNTIME` from configuration
- [ ] Using `GitHub__Token` (double underscores) not `GitHub:Token`
- [ ] Storage connection string configured
- [ ] GitHub settings configured
- [ ] Function App restarted after configuration changes
- [ ] Tested endpoints successfully

---

**Remember:** Flex plan = **NO** `FUNCTIONS_WORKER_RUNTIME` setting!
