# Dashboard API Configuration

## Current Configuration: PRODUCTION FORCED ✅

The dashboard is configured to **always use production API**, even when running on localhost.

## Configuration File

**File:** `DeploymentDashboard/js/config.js`

```javascript
const API_CONFIG = {
	useProd: true,  // ← PRODUCTION FORCED

	productionUrl: 'https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api',
	localUrl: 'http://localhost:7071/api',
};
```

## How to Verify

1. **Refresh browser** (Ctrl+R)
2. **Open Developer Tools** (F12)
3. **Check Console** - You should see:
   ```
   🌐 Using PRODUCTION API: https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api
   ```

## Switch to Local API (If Needed)

If you want to test with local API:

1. **Change config:**
   ```javascript
   useProd: false,  // ← Use local API on localhost
   ```

2. **Start local API:**
   ```powershell
   .\rebuild-and-start.ps1
   ```

3. **Refresh browser**

Console will show:
```
🌐 Using LOCAL API: http://localhost:7071/api
```

## Quick Test Commands

### Test Production API Directly
```powershell
# Test customers endpoint
curl https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/clients/status

# Test CI/CD version
curl https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/cicd/version

# Test latest workflow
curl https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/update-all-customers/latest
```

### Verify in Browser
1. Open http://localhost:8080
2. Press F12 (Developer Tools)
3. Go to Console tab
4. Look for "🌐 Using PRODUCTION API" message

## Configuration Modes

| Mode | useProd | Behavior |
|------|---------|----------|
| **Force Production** | `true` | Always uses production API, even on localhost |
| **Auto-detect** | `false` | Uses local API on localhost, production elsewhere |

## Current Mode: Force Production ✅

```javascript
useProd: true  // Forces production API everywhere
```

This means:
- ✅ Dashboard on localhost → Production API
- ✅ Dashboard on any domain → Production API
- ✅ No need to run local API
- ✅ Always uses live data

## Benefits of Force Production Mode

1. **No Local API Needed**
   - Don't need to run `.\rebuild-and-start.ps1`
   - Don't need Azure Storage Emulator
   - Don't need local configuration

2. **Real Data**
   - See actual deployed data
   - Test with production workflows
   - Verify GitHub integration

3. **Simplified Development**
   - Just start dashboard: `.\start-dashboard.ps1`
   - Open browser: http://localhost:8080
   - Everything works immediately

## When to Use Local API

Switch to local API (`useProd: false`) when:
- Testing new API endpoints
- Debugging API issues
- Development without internet
- Testing with local data

## Troubleshooting

### Dashboard still trying to use localhost:7071?
**Solution:**
1. Hard refresh: Ctrl+Shift+R (clears cache)
2. Check console for "Using PRODUCTION API" message
3. Verify `useProd: true` in config.js

### CORS errors?
**Solution:**
- Azure Functions `host.json` has CORS enabled for all origins (`*`)
- Should work from any domain

### Data not loading?
**Solution:**
1. Test API directly with curl (commands above)
2. Check network tab in DevTools (F12)
3. Verify Azure Functions app is running

## Summary

✅ **Production API forced**  
✅ **No local API needed**  
✅ **Works on localhost**  
✅ **Console shows which API is used**  

**Refresh your browser to see the production data!** 🎉
