# GitHub Integration Setup Summary

## ? What's Working:
- GitHub App configuration: App ID 3906613, Installation ID 136590013
- PEM file location: C:\Users\ArtemKashin\source\repos\DeplomentStatus\DeploymentAPI\.security\github-app-private-key.pem
- PEM file exists and is readable (1675 bytes)
- Azure Functions are running successfully
- API endpoints are responding
- Configuration is properly loaded

## ? Current Issue:
- Getting 401 Unauthorized from GitHub API
- This means GitHub is rejecting the authentication

## ?? Possible Causes:
1. **App ID or Installation ID incorrect** - Double-check at: https://github.com/organizations/AdaptiveBS/settings/apps
2. **App not installed on repository** - Verify at: https://github.com/organizations/AdaptiveBS/settings/installations  
3. **App permissions insufficient** - Need "Actions: Read-only" permission
4. **Wrong PEM file** - Ensure this is the private key for App ID 3906613

## ?? Next Steps:
1. Check Functions console window for detailed error about JWT or token generation
2. Verify App ID at: https://github.com/organizations/AdaptiveBS/settings/apps
3. Verify Installation ID and that app is installed on AdaptiveBS/CIApp
4. Verify app has "Actions" read permission

## ?? Quick Test Scripts Created:
- `restart-and-fix.ps1` - Restart Functions with proper setup
- `test-workflows.ps1` - Test API and display runs
- `get-workflows.ps1` - Get detailed workflow data
- `check-error.ps1` - Check detailed error responses

## ?? Documentation:
- GITHUB-APP-SETUP.md - Complete GitHub App setup guide
- GITHUB-AUTH-COMPARISON.md - PAT vs GitHub App comparison
- GITHUB-PEM-SETUP.md - PEM file configuration guide

Run `Get-Content GITHUB-APP-SETUP.md` to see full setup instructions.
