# 🚀 Azure Deployment Checklist - OpenAPI Update

## Pre-Deployment Verification

- [x] ✅ **Build successful** - Release configuration compiles without errors
- [x] ✅ **OpenAPI spec created** - `DeploymentAPI/openapi.json` exists
- [x] ✅ **OpenAPI endpoint working** - `GET /api/swagger.json` tested locally
- [x] ✅ **GitHub Actions workflow configured** - `.github/workflows/openapi-ci-cd.yml`
- [x] ✅ **Documentation created** - Setup guide and CI/CD integration guide

---

## Deployment Steps

### 1. Commit and Push Changes

```powershell
git add .
git commit -m "feat: add OpenAPI/Swagger specification and CI/CD integration

- Add OpenAPI 3.0.1 specification file
- Add GetOpenApiSpecFunction to serve spec at /api/swagger.json
- Add GitHub Actions workflow for OpenAPI CI/CD
- Add comprehensive documentation for setup and CI/CD integration
- Configure openapi.json to be included in build output"
git push origin develop
```

### 2. Deploy to Azure Functions

**Option A: Visual Studio**
1. Right-click `DeploymentAPI` project
2. Select **Publish**
3. Select existing profile: `func-deployment-status-api-g0egd2dbc9d9c2d9`
4. Click **Publish**
5. Wait for deployment to complete

**Option B: Azure Functions Core Tools**
```powershell
cd DeploymentAPI
func azure functionapp publish func-deployment-status-api-g0egd2dbc9d9c2d9
```

**Option C: GitHub Actions** (if configured)
- Merge to main branch
- GitHub Actions will auto-deploy

### 3. Verify Deployment

**Test the OpenAPI endpoint:**
```powershell
# Test production endpoint
Invoke-WebRequest -Uri "https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/swagger.json" -UseBasicParsing

# Should return 200 OK with JSON content
```

**Or open in browser:**
```
https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/swagger.json
```

---

## Post-Deployment Testing

### Test All Endpoints

```powershell
$baseUrl = "https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net"
$masterKey = "YOUR_MASTER_KEY_HERE"

# 1. OpenAPI Spec (Anonymous - should work without key)
Invoke-WebRequest -Uri "$baseUrl/api/swagger.json" -UseBasicParsing

# 2. Applications (Anonymous)
Invoke-WebRequest -Uri "$baseUrl/api/applications" -UseBasicParsing

# 3. Customers (Anonymous)
Invoke-WebRequest -Uri "$baseUrl/api/customers" -UseBasicParsing

# 4. Clients Status (Requires key)
Invoke-WebRequest -Uri "$baseUrl/api/clients/status?code=$masterKey" -UseBasicParsing

# 5. CI/CD Version (Anonymous)
Invoke-WebRequest -Uri "$baseUrl/api/cicd/version" -UseBasicParsing
```

---

## CI/CD Workflow Testing

### Trigger the OpenAPI Workflow

1. **Push to develop branch:**
   ```bash
   git push origin develop
   ```

2. **Check GitHub Actions:**
   - Go to: https://github.com/artkashin/DeploymentStatus/actions
   - Look for "OpenAPI CI/CD" workflow
   - Verify it completes successfully

3. **Check workflow outputs:**
   - OpenAPI spec validation (Spectral)
   - TypeScript client generation
   - C# client generation
   - Artifacts uploaded

### Merge to Main

1. **Create Pull Request:**
   ```bash
   # From GitHub UI or CLI
   gh pr create --base main --head develop --title "Add OpenAPI specification" --body "Adds OpenAPI/Swagger support for CI/CD-driven API updates"
   ```

2. **Review PR:**
   - Check "API Changes" summary (breaking change detection)
   - Review generated clients in artifacts
   - Verify build passes

3. **Merge:**
   - Merge PR to main
   - Workflow will:
	 - Commit updated spec (if changed)
	 - Deploy documentation to GitHub Pages
	 - Generate and publish clients

---

## Enable GitHub Pages (Optional)

### For API Documentation

1. Go to repository **Settings** → **Pages**
2. Source: **Deploy from a branch**
3. Branch: **gh-pages** / **root**
4. Click **Save**

5. Access documentation at:
   ```
   https://artkashin.github.io/DeploymentStatus/api-docs/api-documentation.html
   ```

---

## Client Generation Examples

### Generate Dashboard Client

```powershell
# TypeScript for DeploymentDashboard
npx @openapitools/openapi-generator-cli generate `
  -i https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/swagger.json `
  -g typescript-fetch `
  -o DeploymentDashboard/generated-api `
  --additional-properties=supportsES6=true,typescriptThreePlus=true
```

### Generate C# Client Library

```powershell
dotnet tool install --global NSwag.ConsoleCore

nswag openapi2csclient `
  /input:https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/swagger.json `
  /classname:DeploymentApiClient `
  /namespace:DeploymentAPI.Client `
  /output:DeploymentApiClient.cs
```

---

## Rollback Plan

If deployment fails:

```powershell
# Revert to previous deployment slot (if using slots)
az functionapp deployment slot swap --resource-group YOUR_RG --name func-deployment-status-api-g0egd2dbc9d9c2d9 --slot staging --target-slot production

# Or redeploy previous version via Visual Studio
```

---

## Dashboard Update (Optional Next Step)

### Update Dashboard to Use Generated Client

1. Generate TypeScript client (see above)
2. Update `DeploymentDashboard/js/api.js` to import generated client
3. Replace manual fetch calls with type-safe client calls
4. Test locally: `.\start-dashboard.ps1`
5. Commit and push

---

## Verification Checklist

After deployment, verify:

- [ ] OpenAPI endpoint returns 200 OK
- [ ] OpenAPI JSON is valid (test with validator)
- [ ] All existing endpoints still work
- [ ] Dashboard still functions correctly
- [ ] GitHub Actions workflow triggered and passed
- [ ] Documentation generated (if using GitHub Pages)
- [ ] Clients can be generated from the spec

---

## Expected Outcomes

✅ **OpenAPI Endpoint Available:**
```
https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/swagger.json
```

✅ **GitHub Actions Workflow Active:**
- Validates OpenAPI on every push
- Generates clients automatically
- Deploys documentation on main merge

✅ **API Contract Version Controlled:**
- Changes tracked in Git
- Breaking changes detected in PRs
- Historical versions available

✅ **Client Generation Ready:**
- TypeScript, C#, Python, and more
- Type-safe API clients
- Automatic updates from spec

---

## Support

If issues occur:

1. **Check function logs:**
   ```powershell
   func azure functionapp logstream func-deployment-status-api-g0egd2dbc9d9c2d9
   ```

2. **Check GitHub Actions logs:**
   - https://github.com/artkashin/DeploymentStatus/actions

3. **Review documentation:**
   - `OPENAPI-SETUP-GUIDE.md`
   - `OPENAPI-CI-CD-INTEGRATION.md`
   - `OPENAPI-IMPLEMENTATION-SUMMARY.md`

---

**🎉 Ready to deploy! Follow the steps above to publish your OpenAPI-enabled API to Azure.**
