# Azure Functions Authentication Issue - SOLUTION

## 🔴 Problem Identified

Your Azure Functions use **`AuthorizationLevel.Function`**, which requires a **function key** in the URL.

### Error You're Seeing
```
401 Unauthorized
```

## ✅ Solution Options

### Option 1: Get Function Key from Azure Portal (Recommended)

1. **Go to Azure Portal**
   - Navigate to your Function App

2. **Get Function Key**
   - Go to: Function App → Functions → (Any function) → Function Keys
   - Copy the `default` key

3. **Update Dashboard to Use Key**

Add `?code=YOUR_FUNCTION_KEY` to API calls.

**Update `DeploymentDashboard/js/api.js`:**

```javascript
async request(endpoint, options = {}) {
	// Add function key to URL
	const separator = endpoint.includes('?') ? '&' : '?';
	const functionKey = 'YOUR_FUNCTION_KEY_HERE';  // Get from Azure Portal
	const url = `${this.baseUrl}${endpoint}${separator}code=${functionKey}`;

	const response = await fetch(url, options);
	// ... rest of code
}
```

### Option 2: Change Authorization Level to Anonymous (For Development)

**⚠️ Warning:** This makes your API publicly accessible without authentication.

Update all functions to use `AuthorizationLevel.Anonymous`:

```csharp
[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "clients/status")]
```

Then redeploy.

### Option 3: Use Azure AD Authentication (Production)

Configure Azure AD for proper authentication.

## 🎯 Quick Fix for Development

### Step 1: Get Your Function Key

```powershell
# Using Azure CLI
az functionapp keys list \
  --name func-deployment-status-api-g0egd2dbc9d9c2d9 \
  --resource-group YOUR_RESOURCE_GROUP \
  --query "functionKeys.default" -o tsv
```

**Or from Azure Portal:**
1. Open https://portal.azure.com
2. Go to Function App: `func-deployment-status-api-g0egd2dbc9d9c2d9`
3. Functions → (select any function) → Function Keys
4. Copy **default** key

### Step 2: Update Config

I'll create an updated config that includes the function key option.

## 📝 Current API Endpoints Need Keys

All these endpoints need `?code=YOUR_KEY`:

```
https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/clients/status?code=YOUR_KEY
https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/cicd/version?code=YOUR_KEY
https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/update-all-customers/latest?code=YOUR_KEY
```

## 🛠️ Next Steps

1. **Get your function key** from Azure Portal
2. **Choose your approach:**
   - Add key to API calls (secure)
   - Change to Anonymous auth (less secure, development only)
3. **Update dashboard code** with the key

## 📖 Documentation

See:
- Azure Functions Security: https://learn.microsoft.com/azure/azure-functions/security-concepts
- Function Keys: https://learn.microsoft.com/azure/azure-functions/functions-bindings-http-webhook-trigger#authorization-keys

---

**That's why dashboard can't connect - it needs the function key!** 🔑
