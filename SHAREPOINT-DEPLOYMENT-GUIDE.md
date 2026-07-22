# SharePoint Deployment - Complete Guide

## ✅ What You Need

### 1. Azure Functions API URL
Your API must be deployed to Azure first. You cannot use `localhost:7071` from SharePoint.

**Deploy API to Azure:**
```powershell
# From Visual Studio
Right-click DeploymentAPI project → Publish → Azure → Azure Functions

# Or using Azure CLI
cd DeploymentAPI
func azure functionapp publish your-function-app-name
```

### 2. Get Your API URL
After deployment, your API URL will be:
```
https://your-function-app-name.azurewebsites.net/api
```

### 3. CORS is Already Configured ✓
The `host.json` now includes SharePoint:
```json
"allowedOrigins": [
	"https://adaptivenav.sharepoint.com",
	"http://localhost:7071",
	"file://"
]
```

## 📋 Deployment Options

### Option A: Single HTML File (Easiest)

Perfect for quick deployment to SharePoint.

**File:** `dashboard-sharepoint-standalone.html`

#### Steps:
1. **Edit the API URL** in the file (line 200):
   ```javascript
   const API_BASE_URL = 'https://your-api.azurewebsites.net/api';
   ```

2. **Upload to SharePoint:**
   - Go to https://adaptivenav.sharepoint.com/sites/KnowledgeBase
   - Navigate to Site Contents → Site Assets (or create "DashboardFiles" library)
   - Upload `dashboard-sharepoint-standalone.html`

3. **Create Page:**
   - New → Page
   - Add "Embed" web part
   - Paste iframe code:
	 ```html
	 <iframe src="/sites/KnowledgeBase/SiteAssets/dashboard-sharepoint-standalone.html" 
			 width="100%" 
			 height="1200px" 
			 frameborder="0">
	 </iframe>
	 ```

4. **Publish the page!**

### Option B: Separate Files (Better for Maintenance)

Use when you need to update CSS/JS independently.

#### File Structure:
```
SharePoint Site Assets/
└── Dashboard/
	├── dashboard.html
	├── css/
	│   └── style.css
	└── js/
		├── config.js
		├── api.js
		└── app.js
```

#### Steps:
1. **Update config.js:**
   ```javascript
   const API_CONFIG = {
	   baseUrl: 'https://your-api.azurewebsites.net/api'
   };
   ```

2. **Upload files** to SharePoint Document Library maintaining folder structure

3. **Update paths** in `dashboard.html`:
   ```html
   <link rel="stylesheet" href="/sites/KnowledgeBase/SiteAssets/Dashboard/css/style.css">
   <script src="/sites/KnowledgeBase/SiteAssets/Dashboard/js/config.js"></script>
   <script src="/sites/KnowledgeBase/SiteAssets/Dashboard/js/api.js"></script>
   <script src="/sites/KnowledgeBase/SiteAssets/Dashboard/js/app.js"></script>
   ```

4. **Embed** using iframe or Script Editor web part

### Option C: SharePoint App Page (Most Integrated)

Create a native SharePoint page with embedded dashboard.

#### Steps:
1. **Create App Page:**
   - Site Contents → Add an app → App Catalog
   - Add "Single Page App Part"

2. **Paste Dashboard HTML** directly into the app part

3. **Configure:**
   - Set page layout
   - Add navigation links
   - Configure permissions

## 🔧 Configuration

### Update API URL

**In standalone file:**
```javascript
// Line ~200 in dashboard-sharepoint-standalone.html
const API_BASE_URL = 'https://YOUR-APP-NAME.azurewebsites.net/api';
```

**In separate config.js:**
```javascript
const API_CONFIG = {
	baseUrl: 'https://YOUR-APP-NAME.azurewebsites.net/api'
};
```

### Test API Connection

Before deploying, test your API in browser:
```
https://YOUR-APP-NAME.azurewebsites.net/api/update-all-customers/latest
```

Should return JSON with workflow data.

## 🚨 Troubleshooting

### CORS Errors in SharePoint

**Error:**
```
Access to fetch at 'https://your-api.azurewebsites.net/api/...' 
from origin 'https://adaptivenav.sharepoint.com' 
has been blocked by CORS policy
```

**Solution:**
1. Verify `host.json` includes SharePoint origin:
   ```json
   "allowedOrigins": [
	   "https://adaptivenav.sharepoint.com"
   ]
   ```

2. Redeploy API to Azure (CORS changes require redeployment)

3. Clear browser cache and retry

### API Not Responding

**Check:**
1. API is deployed and running in Azure
2. Test URL directly in browser
3. Check Azure Function logs for errors
4. Verify GitHub App credentials are configured in Azure

### Dashboard Shows "Loading..." Forever

**Causes:**
1. Wrong API URL in config
2. API not deployed
3. CORS not configured
4. Network blocked by firewall

**Debug:**
1. Open browser console (F12)
2. Look for red errors
3. Check Network tab for failed requests
4. Verify API URL is correct

### SharePoint Security Blocks Script

Some SharePoint tenants block custom scripts.

**Solution:**
1. Request script execution permissions from admin:
   ```powershell
   Set-SPOSite https://adaptivenav.sharepoint.com/sites/KnowledgeBase -DenyAddAndCustomizePages 0
   ```

2. Or use Power Apps custom page instead

## 📊 Expected Results

Once deployed, your SharePoint page will show:

### Live Data from GitHub Actions:
- ✅ Latest "Update all customers" workflow run
- ✅ Customer deployment status (success/failure)
- ✅ Success rate percentage
- ✅ Duration and runner information

### Auto-Refresh:
- Manual: Click "Refresh" button
- Automatic: Add auto-refresh script (optional)

### Color-Coded Cards:
- 🟢 Green: Successful deployments
- 🔴 Red: Failed deployments

## 🔐 Security Considerations

### API Authentication
Current setup uses GitHub App authentication (secure).

### SharePoint Permissions
Dashboard inherits SharePoint page permissions:
- Only users with site access can view
- No additional authentication needed

### CORS Security
For production, limit origins:
```json
"allowedOrigins": [
	"https://adaptivenav.sharepoint.com"
]
```

Remove `"*"`, `"file://"`, and `localhost` entries.

## 📝 Maintenance

### Updating Dashboard
1. Edit files locally
2. Upload new versions to SharePoint
3. Clear browser cache (Ctrl+F5)

### Updating API
1. Make changes to DeploymentAPI project
2. Redeploy to Azure
3. Dashboard automatically uses new version

### Monitoring
1. Check Azure Function logs for errors
2. Monitor API usage in Azure Portal
3. Review SharePoint page analytics

## 🎯 Next Steps

1. ✅ Deploy API to Azure
2. ✅ Get API URL
3. ✅ Update dashboard with API URL
4. ✅ Test API endpoint in browser
5. ✅ Upload dashboard to SharePoint
6. ✅ Create SharePoint page
7. ✅ Test dashboard in SharePoint
8. ✅ Share with users!

## 📞 Support

If you encounter issues:
1. Check browser console (F12) for errors
2. Verify API is accessible
3. Test CORS configuration
4. Check SharePoint script permissions

---

**Ready to deploy? Start with Option A (Single HTML File) for quickest results!**
