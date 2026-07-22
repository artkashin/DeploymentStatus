# Azure Deployment Documentation - Final Updates

## All Issues Resolved ✅

### Issue 1: Colon in Environment Variables ✅ FIXED
**Problem:** Azure Functions environment variables cannot contain colons (`:`)  
**Solution:** Updated all documentation to use double underscores (`__`)

### Issue 2: FUNCTIONS_WORKER_RUNTIME Invalid for Flex Plan ✅ FIXED
**Problem:** Flex Consumption plan doesn't support `FUNCTIONS_WORKER_RUNTIME` setting  
**Solution:** Updated all documentation to clarify plan-specific requirements

## Files Updated

### Major Updates

1. **AZURE-DEPLOYMENT.md** ✅
   - ✅ All `GitHub:*` changed to `GitHub__*` for Azure Portal
   - ✅ Added warning about double underscores
   - ✅ Added section on hosting plan differences
   - ✅ Clarified when to use/omit `FUNCTIONS_WORKER_RUNTIME`
   - ✅ Updated all Azure CLI examples

2. **AZURE-CONFIGURATION.md** ✅
   - ✅ Added critical warning at top
   - ✅ All JSON templates updated with `GitHub__*`
   - ✅ Separated config for Consumption/Premium vs Flex plans
   - ✅ Updated all Azure CLI commands
   - ✅ Added Flex plan troubleshooting entry

3. **AZURE-ENV-VAR-FORMAT.md** ✅
   - ✅ Added section on hosting plan differences
   - ✅ Updated examples for both plan types
   - ✅ Added troubleshooting for Flex plan runtime error
   - ✅ Comparison table for plan types

4. **AZURE-FLEX-PLAN-SETUP.md** ✅ NEW
   - Complete guide for Flex Consumption plan users
   - Explains why FUNCTIONS_WORKER_RUNTIME should be omitted
   - Flex-specific CLI commands
   - Migration guide from standard to Flex
   - Quick troubleshooting checklist

5. **README.md** ✅
   - Added reference to AZURE-FLEX-PLAN-SETUP.md
   - Documentation section updated

6. **DOCUMENTATION-UPDATE-SUMMARY.md** ✅
   - Summary of all changes made

## Configuration Matrix

### For Azure Portal / Azure CLI (Environment Variables)

| Setting Name | Standard/Premium/Dedicated | Flex Consumption | local.settings.json |
|--------------|---------------------------|------------------|---------------------|
| Storage Type | `StorageType` | `StorageType` | `"StorageType"` |
| Worker Runtime | `FUNCTIONS_WORKER_RUNTIME` | ❌ **OMIT** | `"FUNCTIONS_WORKER_RUNTIME"` |
| GitHub Auth Type | `GitHub__AuthType` | `GitHub__AuthType` | `"GitHub:AuthType"` |
| GitHub Token | `GitHub__Token` | `GitHub__Token` | `"GitHub:Token"` |
| GitHub Owner | `GitHub__Owner` | `GitHub__Owner` | `"GitHub:Owner"` |
| GitHub Repository | `GitHub__Repository` | `GitHub__Repository` | `"GitHub:Repository"` |
| GitHub App ID | `GitHub__AppId` | `GitHub__AppId` | `"GitHub:AppId"` |
| Installation ID | `GitHub__InstallationId` | `GitHub__InstallationId` | `"GitHub:InstallationId"` |
| Private Key Source | `GitHub__PrivateKeySource` | `GitHub__PrivateKeySource` | `"GitHub:PrivateKeySource"` |
| Key Vault URL | `GitHub__KeyVaultUrl` | `GitHub__KeyVaultUrl` | `"GitHub:KeyVaultUrl"` |
| Key Secret Name | `GitHub__KeySecretName` | `GitHub__KeySecretName` | `"GitHub:KeySecretName"` |

## Quick Reference by Hosting Plan

### Standard Consumption Plan
```bash
# Azure CLI
az functionapp config appsettings set \
  --settings \
	"FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" \
	"GitHub__Token=ghp_xxx" \
	"GitHub__Owner=AdaptiveBS"
```

### Flex Consumption Plan
```bash
# Azure CLI (NO FUNCTIONS_WORKER_RUNTIME)
az functionapp config appsettings set \
  --settings \
	"GitHub__Token=ghp_xxx" \
	"GitHub__Owner=AdaptiveBS"
```

### Premium Plan
```bash
# Azure CLI (same as Standard)
az functionapp config appsettings set \
  --settings \
	"FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" \
	"GitHub__Token=ghp_xxx" \
	"GitHub__Owner=AdaptiveBS"
```

### Dedicated (App Service Plan)
```bash
# Azure CLI (same as Standard)
az functionapp config appsettings set \
  --settings \
	"FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" \
	"GitHub__Token=ghp_xxx" \
	"GitHub__Owner=AdaptiveBS"
```

## How to Identify Your Plan Type

**Method 1: Azure Portal**
1. Open your Function App
2. Go to **Overview** page
3. Look at "App Service plan" section:
   - Shows "Flex Consumption" → Use Flex config
   - Shows plan name (e.g., "ASP-xxx") → Use Standard config

**Method 2: Azure CLI**
```powershell
az functionapp show \
  --name func-deployment-status-api \
  --resource-group rg-deployment-status \
  --query "kind"
```
- Output contains "FlexConsumption" → Flex plan
- Output is "functionapp" → Standard Consumption
- Output contains "ElasticPremium" → Premium plan

## Common Errors & Solutions

### Error: "FUNCTIONS_WORKER_RUNTIME is invalid"
**Cause:** Using Flex Consumption plan with FUNCTIONS_WORKER_RUNTIME setting  
**Solution:** Remove the setting completely from Azure Portal Configuration

### Error: "GitHub authentication failed"
**Cause:** Using colons instead of double underscores  
**Solution:** Change `GitHub:Token` to `GitHub__Token`

### Error: "Configuration not found"
**Cause:** Settings not applied or app not restarted  
**Solution:** Save settings and restart Function App

## Testing Your Configuration

### Quick Test Script
```powershell
# Set your values
$FUNCTION_APP = "func-deployment-status-api"
$RESOURCE_GROUP = "rg-deployment-status"

# Get function key
$KEY = az functionapp keys list `
  --name $FUNCTION_APP `
  --resource-group $RESOURCE_GROUP `
  --query "functionKeys.default" -o tsv

# Test endpoint
curl "https://$FUNCTION_APP.azurewebsites.net/api/update-all-customers/latest?code=$KEY"
```

### Expected Response
```json
{
  "runId": 123456789,
  "workflowName": "Update all customers",
  "totalCustomers": 8,
  "successfulInstallations": 6,
  "failedInstallations": 2,
  "customers": [...]
}
```

## Migration Checklist

### If Migrating from Standard to Flex Consumption

- [ ] Backup current Function App settings
- [ ] Create new Flex Consumption Function App
- [ ] Copy all settings EXCEPT `FUNCTIONS_WORKER_RUNTIME`
- [ ] Ensure all `GitHub:*` changed to `GitHub__*`
- [ ] Deploy application
- [ ] Test all endpoints
- [ ] Update DNS/Application Gateway if applicable
- [ ] Monitor for 24-48 hours
- [ ] Delete old Function App

### If Fixing Existing Deployment

- [ ] Check hosting plan type (Portal → Overview)
- [ ] If Flex: Remove `FUNCTIONS_WORKER_RUNTIME`
- [ ] Change all `GitHub:*` to `GitHub__*`
- [ ] Save configuration
- [ ] Restart Function App
- [ ] Test endpoints
- [ ] Verify GitHub integration works

## Documentation Structure

```
Root/
├── AZURE-DEPLOYMENT.md ................ Main deployment guide
├── AZURE-CONFIGURATION.md ............. Configuration templates
├── AZURE-ENV-VAR-FORMAT.md ............ Environment variable format rules
├── AZURE-FLEX-PLAN-SETUP.md ........... Flex Consumption plan specific guide
├── DOCUMENTATION-UPDATE-SUMMARY.md .... Summary of changes made
└── README.md .......................... Main project documentation (updated)
```

## Key Takeaways

1. **Azure Portal/CLI:** Use `GitHub__Token` (double underscore)
2. **local.settings.json:** Use `"GitHub:Token"` (colon in JSON)
3. **C# Code:** Use `configuration["GitHub:Token"]` (colon - .NET converts automatically)
4. **Flex Consumption Plan:** Omit `FUNCTIONS_WORKER_RUNTIME` entirely
5. **Other Plans:** Include `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`

## Build Status

✅ **Build successful** - All changes compile correctly  
✅ **No code changes required** - Only documentation updates  
✅ **Backward compatible** - Existing code works with both formats

## Next Steps for Users

1. **Read:** `AZURE-ENV-VAR-FORMAT.md` for environment variable rules
2. **Check:** Your hosting plan type
3. **Choose:** 
   - Flex Plan? Read `AZURE-FLEX-PLAN-SETUP.md`
   - Other Plan? Follow `AZURE-DEPLOYMENT.md`
4. **Configure:** Use correct format for your plan
5. **Deploy:** Using Visual Studio or Azure CLI
6. **Test:** Verify endpoints work

## Support

If you encounter issues:
1. Check your hosting plan type first
2. Verify environment variable format (double underscores)
3. Ensure Function App has been restarted
4. Review logs in Azure Portal → Log stream
5. Consult troubleshooting sections in each guide

---

**All documentation is now accurate and complete!** ✅
