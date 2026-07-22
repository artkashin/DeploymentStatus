# GitHub App Authentication Troubleshooting Summary

## Issue
Azure Functions returns **401 Unauthorized** when trying to access GitHub API using GitHub App authentication.

## Diagnostics Run
? PEM file exists and has correct format (28 lines, valid RSA header/footer)
? PEM file can be decoded from base64 (1191 bytes)
? Configuration file is valid
? Azure Functions process is running
? GitHub API returns 401 Unauthorized

## Configuration
- **App ID**: 3906816
- **Installation ID**: 137493603
- **Owner**: AdaptiveBS
- **Repository**: CIApp
- **PEM Path**: C:\Users\ArtemKashin\source\repos\DeplomentStatus\DeploymentAPI\.security\github-app-private-key.pem

## Root Cause Analysis
The 401 Unauthorized error indicates that GitHub is rejecting the authentication token. This can happen for several reasons:

### 1. Wrong App ID
The App ID in `local.settings.json` doesn't match the actual GitHub App.

**Verify**: https://github.com/settings/apps
- Click on your app
- Check "App ID" field matches: **3906816**

### 2. Wrong Installation ID
The Installation ID is incorrect or the app was uninstalled/reinstalled (which changes the ID).

**Verify**: 
- https://github.com/organizations/AdaptiveBS/settings/installations (for org)
- OR https://github.com/settings/installations (for personal)
- Click "Configure" on your app
- URL will show: `.../installations/INSTALLATION_ID`
- Should be: **137493603**

### 3. Private Key Mismatch (MOST LIKELY)
The private key file doesn't match the key configured in GitHub App, or it was revoked.

**Verify**: https://github.com/settings/apps
- Click on your app
- Scroll to "Private keys" section
- Check if a key exists and when it was created
- If the key was regenerated after you copied it, you need to download the new one

### 4. Missing Permissions
The GitHub App doesn't have the required permissions.

**Required Permissions**:
- **Actions**: Read-only (minimum)
- **Contents**: Read-only (minimum)
- **Metadata**: Read-only (auto-included)

**Verify**: https://github.com/settings/apps
- Click on your app
- Check "Repository permissions" section

### 5. Repository Not Accessible
The app is not installed on the target repository or doesn't have access to it.

**Verify**: https://github.com/settings/installations
- Click "Configure" on your app
- Under "Repository access":
  - Either "All repositories" is selected
  - OR "Only select repositories" includes **AdaptiveBS/CIApp**

## Solution Steps

### Option 1: Regenerate Private Key (Recommended)
This is the most common fix:

1. Go to https://github.com/settings/apps
2. Click on your app (App ID: 3906816)
3. Scroll to "Private keys"
4. Click "Generate a private key"
5. Download the `.pem` file
6. Copy it to: `DeploymentAPI\.security\github-app-private-key.pem`
   ```powershell
   Copy-Item "C:\Path\To\Downloaded\key.pem" "DeploymentAPI\.security\github-app-private-key.pem" -Force
   ```
7. Restart Functions:
   ```powershell
   .\restart-and-fix.ps1
   ```
8. Wait 15-20 seconds for startup
9. Test:
   ```powershell
   .\test-workflows.ps1
   ```

### Option 2: Verify/Update IDs

1. **Get App ID**:
   - Visit: https://github.com/settings/apps
   - Click your app
   - Note the "App ID" value

2. **Get Installation ID**:
   - Visit: https://github.com/organizations/AdaptiveBS/settings/installations
   - Click "Configure" on your app
   - Get ID from URL: `.../installations/INSTALLATION_ID`

3. **Update local.settings.json** if IDs are wrong:
   ```json
   {
     "Values": {
       "GitHub:AppId": "YOUR_APP_ID",
       "GitHub:InstallationId": "YOUR_INSTALLATION_ID"
     }
   }
   ```

4. **Restart Functions**:
   ```powershell
   .\restart-and-fix.ps1
   ```

### Option 3: Switch to PAT (Temporary Workaround)
If you need to test quickly while debugging GitHub App issues:

1. Generate a Personal Access Token:
   - Visit: https://github.com/settings/tokens
   - Click "Generate new token (classic)"
   - Select scopes: `repo`, `workflow`
   - Copy the token

2. Update `local.settings.json`:
   ```json
   {
     "Values": {
       "GitHub:AuthType": "PAT",
       "GitHub:Token": "ghp_your_token_here",
       "GitHub:Owner": "AdaptiveBS",
       "GitHub:Repository": "CIApp"
     }
   }
   ```

3. Restart Functions:
   ```powershell
   .\restart-and-fix.ps1
   ```

## Verification Checklist

Run through this checklist to verify everything:

- [ ] App ID matches: https://github.com/settings/apps
- [ ] Installation ID matches: https://github.com/settings/installations
- [ ] Private key exists: `DeploymentAPI\.security\github-app-private-key.pem`
- [ ] Private key is active (not revoked) in GitHub App settings
- [ ] GitHub App has "Actions" permission (Read-only minimum)
- [ ] GitHub App has "Contents" permission (Read-only minimum)
- [ ] GitHub App is installed on AdaptiveBS/CIApp repository
- [ ] Azure Functions process is running
- [ ] Functions can read the PEM file

## Testing Commands

```powershell
# 1. Verify configuration
pwsh -File verify-github-config.ps1

# 2. Diagnose authentication
pwsh -File diagnose-github-auth.ps1

# 3. Restart and test
.\restart-and-fix.ps1
Start-Sleep -Seconds 15
.\test-workflows.ps1
```

## Expected Success Output

When working correctly, you should see:
```
Testing GitHub API...

? SUCCESS! Retrieved X workflow runs

=== RECENT WORKFLOW RUNS ===
? MM-DD HH:MM | Workflow Name
   Status: completed | Conclusion: success
   ?? https://github.com/AdaptiveBS/CIApp/actions/runs/...
```

## Additional Debugging

### Check Functions Console
The Functions console window (black window) should show:
```
?? Using file system for GitHub App private key
?? Using GitHub App authentication
? GitHub integration configured
```

If you see errors like:
- `Failed to parse GitHub App private key` ? Private key format issue
- `GitHub:AppId is not configured` ? Configuration not loaded
- `Failed to get installation access token` ? Wrong IDs or key

### Enable Detailed Logging
The `host.json` has been updated to show more detailed logs. Check the Functions console for specific error messages from the `GitHubAppAuthProvider` and `GitHubService`.

## Common Error Messages

| Error | Meaning | Fix |
|-------|---------|-----|
| `401 Unauthorized` | GitHub rejected auth | Regenerate key, verify IDs |
| `404 Not Found` | Installation not found | Verify Installation ID |
| `403 Forbidden` | Missing permissions | Add permissions to GitHub App |
| `Resource not accessible by integration` | App not installed on repo | Install app on repository |
| `Failed to parse GitHub App private key` | Invalid PEM format | Download new key |

## Contact

If the issue persists after following these steps:
1. Check the Functions console for the specific error message
2. Verify all steps in the checklist above
3. Consider using PAT authentication as a temporary workaround
