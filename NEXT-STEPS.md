# GitHub App Authentication - Next Steps

## Current Status: 401 Unauthorized ?

Your Azure Functions are running and responding, but GitHub is rejecting the authentication with a **401 Unauthorized** error.

## What We Know
- ? PEM file exists and has valid format
- ? Configuration file is properly structured  
- ? Azure Functions process is running
- ? GitHub API authentication failing

## Most Likely Cause
The **private key doesn't match** the one configured in your GitHub App. This happens when:
- The key was regenerated in GitHub but not updated locally
- The wrong key file was copied
- The key has been revoked

## Quick Fix (Recommended)

Run this command to automatically fix the private key issue:

```powershell
.\fix-github-key.ps1
```

This script will:
1. Help you generate a new private key from GitHub
2. Automatically detect and copy the downloaded key
3. Restart Azure Functions  
4. Test the connection

## Manual Fix

If you prefer to do it manually:

1. **Generate new key**:
   - Visit: https://github.com/settings/apps
   - Click on your app (App ID: 3906816)
   - Scroll to "Private keys"
   - Click "Generate a private key"
   - Download the `.pem` file

2. **Replace the key**:
   ```powershell
   Copy-Item "C:\Path\To\Downloaded\key.pem" "DeploymentAPI\.security\github-app-private-key.pem" -Force
   ```

3. **Restart Functions**:
   ```powershell
   .\restart-and-fix.ps1
   ```

4. **Test**:
   ```powershell
   .\test-workflows.ps1
   ```

## Other Possible Issues

If regenerating the key doesn't work, check:

### 1. App ID and Installation ID
```powershell
# Current values:
# App ID: 3906816
# Installation ID: 137493603

# Verify at:
# - App ID: https://github.com/settings/apps (click your app)
# - Installation ID: https://github.com/settings/installations (click Configure, check URL)
```

### 2. Repository Access
```powershell
# Ensure your GitHub App has access to: AdaptiveBS/CIApp
# Check at: https://github.com/settings/installations
# Click "Configure" on your app
# Verify "AdaptiveBS/CIApp" is in the repository list
```

### 3. Permissions
```powershell
# Required permissions:
# - Actions: Read-only (minimum)
# - Contents: Read-only (minimum)
# Check at: https://github.com/settings/apps (click your app, check "Repository permissions")
```

## Alternative: Use PAT (Temporary)

If you need to continue testing while debugging GitHub App:

1. Generate PAT: https://github.com/settings/tokens
2. Update `local.settings.json`:
   ```json
   {
     "Values": {
       "GitHub:AuthType": "PAT",
       "GitHub:Token": "ghp_your_token_here"
     }
   }
   ```
3. Restart: `.\restart-and-fix.ps1`

## Diagnostic Scripts

| Script | Purpose |
|--------|---------|
| `verify-github-config.ps1` | Check configuration and provide manual verification steps |
| `diagnose-github-auth.ps1` | Run comprehensive diagnostics on PEM file and API |
| `fix-github-key.ps1` | **Interactive script to replace private key** |
| `restart-and-fix.ps1` | Restart Functions with PEM file verification |
| `test-workflows.ps1` | Test the GitHub API connection |

## Full Documentation

See `GITHUB-AUTH-TROUBLESHOOTING.md` for complete troubleshooting guide.

## What to Do Now

**Option 1 - Quick Fix (Recommended)**:
```powershell
.\fix-github-key.ps1
```

**Option 2 - Manual Investigation**:
```powershell
.\verify-github-config.ps1
# Follow the manual verification steps
# Then manually replace the key and restart
```

**Option 3 - Switch to PAT**:
```powershell
# Edit local.settings.json to use PAT
# Then run:
.\restart-and-fix.ps1
```

---

**Most Common Solution**: The private key needs to be regenerated and copied from GitHub. Run `.\fix-github-key.ps1` to fix this automatically.
