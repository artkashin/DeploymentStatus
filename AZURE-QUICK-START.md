# Azure Functions Deployment - Quick Start Card

## 🚀 Deploy in 5 Steps

### Step 1️⃣: Identify Your Hosting Plan
- Azure Portal → Function App → Overview → Look at "App Service plan"
- **Says "Flex Consumption"?** → Use Flex config (skip FUNCTIONS_WORKER_RUNTIME)
- **Says plan name or "Consumption"?** → Use Standard config (include FUNCTIONS_WORKER_RUNTIME)

### Step 2️⃣: Use Correct Environment Variable Format
| ❌ Wrong | ✅ Correct |
|---------|-----------|
| `GitHub:Token` | `GitHub__Token` |
| `GitHub:Owner` | `GitHub__Owner` |
| `GitHub:Repository` | `GitHub__Repository` |

**Golden Rule:** Double underscores (`__`) in Azure Portal, colons (`:`) in local.settings.json

### Step 3️⃣: Configure Application Settings

**Azure Portal → Configuration → Application settings**

#### For Standard/Premium/Dedicated Plans:
```
AzureWebJobsStorage          = <your-storage-connection-string>
StorageType                  = TableStorage
FUNCTIONS_WORKER_RUNTIME     = dotnet-isolated

GitHub__AuthType             = PAT
GitHub__Token                = ghp_your_token_here
GitHub__Owner                = AdaptiveBS
GitHub__Repository           = CIApp
```

#### For Flex Consumption Plan:
```
AzureWebJobsStorage          = <your-storage-connection-string>
StorageType                  = TableStorage
(DO NOT ADD FUNCTIONS_WORKER_RUNTIME)

GitHub__AuthType             = PAT
GitHub__Token                = ghp_your_token_here
GitHub__Owner                = AdaptiveBS
GitHub__Repository           = CIApp
```

### Step 4️⃣: Publish

**Via Visual Studio:**
1. Right-click DeploymentAPI project → Publish
2. Import Profile: `DeploymentAPI/Properties/PublishProfiles/Azure.pubxml`
3. Update placeholders with your Azure details
4. Click Publish

**Via Azure CLI:**
```powershell
cd DeploymentAPI
dotnet publish -c Release -o ./publish
cd publish
Compress-Archive -Path * -DestinationPath ../deploy.zip -Force
az functionapp deployment source config-zip \
  --resource-group rg-deployment-status \
  --name func-deployment-status-api \
  --src ../deploy.zip
```

### Step 5️⃣: Test

```bash
# Get function key from Azure Portal
# Function App → Functions → GetLatestUpdateCustomersStatus → Function Keys → default

curl "https://your-function-app.azurewebsites.net/api/update-all-customers/latest?code=YOUR_KEY"
```

## 🆘 Common Issues

| Issue | Solution |
|-------|----------|
| "FUNCTIONS_WORKER_RUNTIME is invalid" | Remove setting (Flex plan) |
| "GitHub authentication failed" | Change `GitHub:Token` to `GitHub__Token` |
| "Configuration not found" | Restart Function App |
| Functions not showing | Check runtime setting for your plan type |

## 📚 Documentation

| File | Purpose |
|------|---------|
| `AZURE-DEPLOYMENT.md` | Complete deployment guide |
| `AZURE-FLEX-PLAN-SETUP.md` | Flex Consumption plan specific |
| `AZURE-ENV-VAR-FORMAT.md` | Environment variable format rules |
| `AZURE-CONFIGURATION.md` | Configuration templates |

## ✅ Deployment Checklist

- [ ] Identified hosting plan type (Flex or Standard/Premium/Dedicated)
- [ ] Used double underscores (`__`) for all GitHub settings
- [ ] Omitted FUNCTIONS_WORKER_RUNTIME (Flex) or set to dotnet-isolated (others)
- [ ] Configured storage connection string
- [ ] Set GitHub authentication (PAT or App)
- [ ] Published application
- [ ] Restarted Function App
- [ ] Tested endpoints successfully
- [ ] GitHub integration working

## 🎯 Decision Tree

```
Start
  |
  ├─ Is hosting plan "Flex Consumption"?
  |    ├─ YES → Use AZURE-FLEX-PLAN-SETUP.md
  |    |        DON'T set FUNCTIONS_WORKER_RUNTIME
  |    |
  |    └─ NO → Use AZURE-DEPLOYMENT.md (standard guide)
  |             SET FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
  |
  └─ For ALL plans:
	   - Use GitHub__Token (double underscore) in Azure Portal
	   - Use "GitHub:Token" (colon) in local.settings.json
	   - Restart Function App after config changes
```

## 🔗 Quick Links

- Create resources: [Azure Portal](https://portal.azure.com)
- GitHub tokens: [GitHub Settings → Developer settings](https://github.com/settings/tokens)
- Storage Explorer: [Azure Storage Explorer](https://azure.microsoft.com/features/storage-explorer/)

---

**Need help?** Check the troubleshooting sections in the detailed guides!
