# Dashboard Configuration Update

## Production API Configuration

The deployment dashboard has been configured to use your published Azure Functions API.

### Production URL
```
https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api
```

## Files Updated

All dashboard files now automatically detect the environment:

1. **js/config.js** - Main configuration file
   - Automatically uses production URL when not on localhost
   - Uses local API (http://localhost:7071/api) for development

2. **test-simple.html** - Simple test page
   - Auto-detects environment
   - Shows which API URL is being used

3. **test-workflow-visual.html** - Workflow status test page
   - Auto-detects environment
   - Displays API URL in use

4. **dashboard-sharepoint-standalone.html** - SharePoint standalone version
   - Auto-detects environment
   - Logs API URL to console

5. **README.md** - Updated with production URL

## How It Works

The configuration automatically switches based on hostname:

```javascript
const API_URL = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
	? 'http://localhost:7071/api'  // Local development
	: 'https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api';  // Production
```

## Testing

### Local Development
1. Start local API: `.\rebuild-and-start.ps1`
2. Open dashboard: `http://localhost:8080`
3. Dashboard uses: `http://localhost:7071/api`

### Production
1. Deploy dashboard to Azure Static Web Apps
2. Open production URL
3. Dashboard uses: `https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api`

## Quick Test

### Test Production API Directly

```powershell
# Test the customers endpoint
curl https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/customers

# Test latest workflow status
curl https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/update-all-customers/latest
```

### Test Dashboard Pages

1. **Main Dashboard**
   - File: `DeploymentDashboard/index.html`
   - Open in browser
   - Check browser console (F12) for API URL

2. **Simple Test Page**
   - File: `DeploymentDashboard/test-simple.html`
   - Click "Test Workflow Endpoint" button
   - Shows which API URL is being used

3. **Workflow Visual Test**
   - File: `DeploymentDashboard/test-workflow-visual.html`
   - Automatically loads from correct API
   - Displays customer status visually

## Available Endpoints

Your production API endpoints:

| Endpoint | URL |
|----------|-----|
| All Clients Status | `https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/customers` |
| Client Status | `https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/clients/{clientId}/status` |
| CI/CD Version | `https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/cicd/version` |
| Latest Workflow | `https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/update-all-customers/latest` |
| Workflow Run Status | `https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/workflow-runs/{runId}/customer-status` |
| GitHub Workflows | `https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/github/workflows` |
| GitHub Workflow Runs | `https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/github/workflow-runs` |

## CORS Configuration

⚠️ **Important:** Make sure CORS is configured in your Azure Functions to allow requests from your dashboard domain.

**Current Configuration:** `host.json` has CORS enabled for `*` (all origins)

```json
{
  "cors": {
	"allowCredentials": false,
	"allowedOrigins": ["*"]
  }
}
```

**For Production:** Update to specific domain:

```json
{
  "cors": {
	"allowCredentials": false,
	"allowedOrigins": [
	  "https://your-dashboard.azurestaticapps.net"
	]
  }
}
```

## Troubleshooting

### Dashboard shows "Loading..." forever
**Check:**
1. Open browser console (F12)
2. Look for network errors or CORS issues
3. Verify API URL is correct
4. Test API endpoint directly with curl/Postman

### "Failed to fetch" error
**Possible causes:**
1. CORS not configured properly
2. Azure Functions not running
3. Network/firewall blocking requests
4. Function keys required but not provided

### Different URL when deployed
**Solution:**
- The auto-detection only checks for localhost
- If your production URL is different, update `config.js`:

```javascript
const API_CONFIG = {
	baseUrl: window.location.hostname.includes('yourdomain.com')
		? 'https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api'
		: window.location.hostname === 'localhost'
		  ? 'http://localhost:7071/api'
		  : 'https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api'
};
```

## Next Steps

1. ✅ Dashboard configured for production API
2. ⏭️ Deploy dashboard to Azure Static Web Apps
3. ⏭️ Configure custom domain (optional)
4. ⏭️ Update CORS to specific domain (recommended for production)
5. ⏭️ Test all functionality end-to-end

## Deployment Commands

### Deploy Dashboard to Azure Static Web Apps

```bash
# Using Azure Static Web Apps CLI
cd DeploymentDashboard
swa deploy --app-location . --output-location .

# Or via Azure Portal
# 1. Create Static Web App
# 2. Connect to GitHub repository
# 3. Set app location: /DeploymentDashboard
# 4. Build command: (none - static files)
# 5. Output location: .
```

### Deploy API Updates

Already deployed to:
```
https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net
```

To redeploy after changes:
- Via Visual Studio: Right-click → Publish
- Via GitHub Actions: Push to main/develop branch
- Via Azure CLI: See AZURE-DEPLOYMENT.md

---

**Configuration complete!** 🎉 The dashboard will automatically use the correct API based on where it's running.
