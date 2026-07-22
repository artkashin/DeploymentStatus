# Dashboard Running - Quick Reference

## ✅ Dashboard is Live!

**Dashboard URL:** http://localhost:8080  
**API URL (Local):** http://localhost:7071/api  
**API URL (Production):** https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api

## 🌐 Open in Browser

Click or copy this URL:
```
http://localhost:8080
```

## 🎯 What You'll See

The dashboard will show:
- **CI/CD Version** - Current version in production
- **Client Status** - All clients and their deployment status
- **Version Comparison** - Which clients need updates
- **Deployment History** - Recent deployment activity

## 🔄 The Dashboard Auto-Detects Environment

Since you're running on **localhost**, it will use:
- **Local API** (if API is running locally): `http://localhost:7071/api`

When deployed to production, it automatically switches to:
- **Production API**: `https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api`

## 🚀 Start API (if needed)

If you want to test with local API instead of production:

**Terminal 1 - API:**
```powershell
.\rebuild-and-start.ps1
# API runs on http://localhost:7071
```

**Terminal 2 - Dashboard:** (Already running)
```powershell
# Dashboard runs on http://localhost:8080
```

## 🧪 Test Pages

Quick test pages are available:
- **Simple Test:** http://localhost:8080/test-simple.html
- **Workflow Visual:** http://localhost:8080/test-workflow-visual.html
- **SharePoint Test:** http://localhost:8080/dashboard-sharepoint-standalone.html

## 📊 Available Features

### Main Dashboard Features
1. **Real-time Status Overview**
   - Total clients
   - Up-to-date count
   - Outdated count

2. **CI/CD Version Management**
   - View current version
   - Update version with notes
   - Track who updated and when

3. **Client Status Grid**
   - See all clients
   - Applications per client
   - Version status (✓ up-to-date, ⚠ outdated)
   - Deployment history

4. **Deployment History**
   - Chronological deployment log
   - Filter by client/application
   - Status indicators

5. **GitHub Workflow Status** (if configured)
   - Latest "Update all customers" run
   - Customer installation success/failure
   - Runner information
   - Execution times

## 🛠️ Development Commands

### Start Dashboard Only
```powershell
.\start-dashboard.ps1
```

### Start Full Stack (API + Dashboard)
```powershell
.\start-full-stack.ps1
```

### Start API Only
```powershell
.\rebuild-and-start.ps1
```

### Stop Dashboard
Press `Ctrl+C` in the terminal where dashboard is running

## 🔧 Configuration

Dashboard automatically uses correct API based on where it's running:

**File:** `DeploymentDashboard/js/config.js`

```javascript
const API_CONFIG = {
	baseUrl: window.location.hostname === 'localhost'
		? 'http://localhost:7071/api'  // Local
		: 'https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api'  // Production
};
```

## 🌍 Access from Other Devices

If you want to access from other devices on your network:

1. Find your IP address:
   ```powershell
   ipconfig
   # Look for IPv4 Address (e.g., 192.168.1.100)
   ```

2. Access from other device:
   ```
   http://192.168.1.100:8080
   ```

## 📱 Responsive Design

The dashboard is responsive and works on:
- ✅ Desktop browsers
- ✅ Tablets
- ✅ Mobile phones

## 🎨 Browser Support

Tested and working on:
- ✅ Chrome/Edge (Chromium)
- ✅ Firefox
- ✅ Safari

## 🐛 Troubleshooting

### Dashboard shows "Loading..." forever
1. **Check API is accessible:**
   ```powershell
   # Test production API
   curl https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/customers

   # Test local API (if running)
   curl http://localhost:7071/api/customers
   ```

2. **Check browser console (F12)**
   - Look for errors
   - Check which API URL is being used
   - Look for CORS errors

### Port 8080 already in use
**Solution:**
```powershell
# Use different port
cd DeploymentDashboard
python -m http.server 8081  # Use port 8081 instead
```

Then access: http://localhost:8081

### Python not found
**Install Python:**
1. Download from: https://www.python.org/downloads/
2. Run installer
3. ✅ Check "Add Python to PATH"
4. Install
5. Restart terminal

**Alternative - Use Node.js:**
```powershell
# Install http-server globally
npm install -g http-server

# Run dashboard
cd DeploymentDashboard
http-server -p 8080
```

### Data not showing
**Check:**
1. API is accessible
2. Storage has data (use storage explorer or make test deployments)
3. GitHub configuration is correct (if using workflow features)

## 📖 Documentation

- **Dashboard Config:** `DASHBOARD-PRODUCTION-CONFIG.md`
- **Dashboard README:** `DeploymentDashboard/README.md`
- **API Documentation:** `DeploymentAPI/README.md`
- **Workflow API:** `DeploymentAPI/WORKFLOW-CUSTOMER-STATUS-API.md`

## 🔗 Quick Links

| Resource | URL |
|----------|-----|
| Dashboard (Local) | http://localhost:8080 |
| API (Local) | http://localhost:7071/api |
| API (Production) | https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api |
| Test Page | http://localhost:8080/test-simple.html |
| Workflow Visual | http://localhost:8080/test-workflow-visual.html |

## 💡 Tips

1. **Keep both terminals open**
   - Terminal 1: Dashboard server (port 8080)
   - Terminal 2: API server (port 7071) - optional

2. **Use browser DevTools (F12)**
   - Console tab: See API requests and responses
   - Network tab: Monitor API calls
   - Application tab: Check if data is cached

3. **Auto-refresh**
   - Dashboard has a Refresh button
   - Click to reload data manually
   - Or add auto-refresh logic to `app.js`

4. **Test with production data**
   - Dashboard configured to use production API
   - No local API needed if testing with production
   - Just open dashboard and it connects to Azure

---

## 🎉 You're All Set!

Dashboard is running and configured for both local and production use. Open http://localhost:8080 in your browser to start!
