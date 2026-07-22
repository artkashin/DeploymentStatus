# Documentation Update Summary

## Changes Made: Azure Environment Variable Format Correction

Date: 2024
Issue: Azure Functions environment variables cannot contain colons (`:`)
Solution: Updated all Azure deployment documentation to use double underscores (`__`)

## Files Updated

### ✅ Primary Deployment Documentation (CORRECTED)

1. **AZURE-DEPLOYMENT.md**
   - Updated all configuration tables to use `GitHub__Token`, `GitHub__Owner`, etc.
   - Added warning box about double underscore requirement
   - Updated Azure CLI command examples
   - Kept colons in local.settings.json examples (correct for JSON)
   - Changes: 4 sections updated

2. **AZURE-CONFIGURATION.md**
   - Added critical warning at the top of document
   - Updated all JSON configuration templates for Azure Portal
   - Updated all Azure CLI command examples
   - Updated environment-specific settings (staging/production)
   - Updated Key Vault reference examples
   - Updated troubleshooting section
   - Changes: 8 sections updated

3. **AZURE-ENV-VAR-FORMAT.md** (NEW)
   - Created comprehensive reference guide
   - Explains why double underscores are required
   - Provides quick reference table
   - Shows correct vs incorrect examples
   - Includes troubleshooting steps

4. **README.md**
   - Added reference to AZURE-ENV-VAR-FORMAT.md in documentation section
   - Highlighted as "IMPORTANT"

### ⚠️ Other Documentation Files (UNCHANGED - Correct As-Is)

The following files contain `GitHub:Token` examples, but these are **correct** because they document `local.settings.json` format (JSON), where colons work fine:

- `DeploymentAPI/GITHUB-INTEGRATION.md` - Local settings examples
- `DeploymentAPI/README.md` - Local settings examples
- `GITHUB-APP-SETUP.md` - Local settings examples
- `GITHUB-AUTH-COMPARISON.md` - Local settings examples
- `GITHUB-AUTH-TROUBLESHOOTING.md` - Local settings examples
- `GITHUB-QUICK-REFERENCE.md` - Local settings examples
- `GITHUB-SETUP-GUIDE.md` - Local settings examples
- `HOW-TO-RUN.md` - Local settings examples
- `NEXT-STEPS.md` - Local settings examples

**These files do NOT need updating** because they document local development configuration.

## Key Points for Users

### For Azure Portal Configuration

**Always use double underscores:**
```
GitHub__AuthType
GitHub__Token
GitHub__Owner
GitHub__Repository
GitHub__AppId
GitHub__InstallationId
GitHub__PrivateKeySource
GitHub__KeyVaultUrl
GitHub__KeySecretName
```

### For local.settings.json

**Keep using colons:**
```json
{
  "Values": {
	"GitHub:AuthType": "PAT",
	"GitHub:Token": "ghp_xxx",
	"GitHub:Owner": "AdaptiveBS",
	"GitHub:Repository": "CIApp"
  }
}
```

### For .NET Code

**No changes needed:**
```csharp
var token = configuration["GitHub:Token"];  // Works with both formats
var owner = configuration["GitHub:Owner"];
```

The .NET configuration system automatically translates `GitHub__Token` (environment variable) to `GitHub:Token` (configuration key).

## Configuration Sections Updated

### AZURE-DEPLOYMENT.md
- Line ~54-72: Application settings table
- Line ~163-167: Azure CLI deployment commands
- Line ~240-260: Configuration reference tables

### AZURE-CONFIGURATION.md
- Added warning at top
- Lines ~20-30: Required settings JSON
- Lines ~35-45: GitHub PAT configuration
- Lines ~50-70: GitHub App configuration
- Lines ~105-125: Azure CLI PAT commands
- Lines ~135-160: Azure CLI GitHub App commands
- Lines ~170-190: Environment-specific settings
- Lines ~220-240: Key Vault references
- Line ~300: Troubleshooting section

## Testing

✅ Build successful - no code changes required
✅ Configuration format documented correctly
✅ Warning messages added to prevent mistakes
✅ Quick reference guide created

## Migration Guide for Existing Deployments

If you already have a Function App deployed with incorrect settings (using colons):

1. Go to Azure Portal → Your Function App → Configuration
2. Delete old settings with colons:
   - `GitHub:Token`
   - `GitHub:Owner`
   - `GitHub:Repository`
   - `GitHub:AuthType`

3. Add new settings with double underscores:
   - `GitHub__Token` = <your-token>
   - `GitHub__Owner` = AdaptiveBS
   - `GitHub__Repository` = CIApp
   - `GitHub__AuthType` = PAT

4. Click **Save**
5. **Restart** the Function App
6. Verify GitHub authentication works in logs

## Quick Verification

After updating settings, test with:
```bash
curl "https://your-function-app.azurewebsites.net/api/update-all-customers/latest?code=xxx"
```

If you get GitHub authentication errors, check that:
- Settings use `__` not `:`
- Function App has been restarted
- Token is valid and has `repo` scope

## Summary

✅ **Documentation corrected** for Azure Portal configuration  
✅ **New reference guide** created (AZURE-ENV-VAR-FORMAT.md)  
✅ **Build successful** - no code changes needed  
✅ **Local development** documentation unchanged (correct as-is)  

The distinction is clear:
- **Azure Portal/CLI** → `GitHub__Token` (double underscore)
- **local.settings.json** → `"GitHub:Token"` (colon)
- **C# Code** → `configuration["GitHub:Token"]` (colon, works with both)
