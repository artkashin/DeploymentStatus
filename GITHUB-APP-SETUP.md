# GitHub App Setup Guide

## Why Use GitHub App Instead of Personal Access Token?

### GitHub App Advantages ?

| Feature | GitHub App | Personal Access Token |
|---------|------------|----------------------|
| **Rate Limit** | 15,000 requests/hour | 5,000 requests/hour |
| **Token Lifetime** | 1 hour (auto-rotated) | No expiration or long-lived |
| **User Dependency** | ? Not tied to user | ? Tied to user account |
| **Permissions** | Fine-grained, specific | Broad scope (repo = all access) |
| **Audit Trail** | Better tracking | Limited |
| **Management** | Organization-level | User-level |
| **Security** | ? More secure | ?? Higher risk if leaked |
| **Best for** | Production | Development/Testing |

### Personal Access Token Still Has Access ?

Yes, a PAT with `repo` scope **does have access** to Actions details including:
- Workflow runs
- Job details
- Artifacts
- Logs
- All repository data

But **GitHub App is recommended for production** due to better security and higher rate limits.

## Setup Instructions

### Option 1: Personal Access Token (Quick Setup for Development)

**Use for:** Development, testing, quick prototyping

1. Go to https://github.com/settings/tokens
2. Click "Generate new token (classic)"
3. Select `repo` scope
4. Copy the token
5. Update `local.settings.json`:

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

### Option 2: GitHub App (Recommended for Production)

**Use for:** Production, CI/CD, organization-wide access

#### Step 1: Create GitHub App

1. Go to your organization settings:
   - https://github.com/organizations/AdaptiveBS/settings/apps
   - Or: GitHub ? Organization ? Settings ? Developer settings ? GitHub Apps

2. Click **"New GitHub App"**

3. Configure the app:

   **Basic Information:**
   - **GitHub App name**: `DeploymentAPI-CIApp` (must be unique across GitHub)
   - **Homepage URL**: Your application URL or `https://github.com/AdaptiveBS/CIApp`
   - **Webhook**: Uncheck "Active" (we don't need webhooks for now)

   **Permissions - Repository permissions:**
   - **Actions**: `Read-only` ? (to read workflow runs)
   - **Contents**: `Read-only` (optional, for repository access)
   - **Metadata**: `Read-only` (automatically selected)

   **Where can this GitHub App be installed?**
   - Select: **"Only on this account"**

4. Click **"Create GitHub App"**

5. **Note down the App ID** (you'll see it on the next page)

#### Step 2: Generate Private Key

1. After creating the app, scroll down to **"Private keys"**
2. Click **"Generate a private key"**
3. A `.pem` file will download automatically
4. **Keep this file secure!** This is your authentication credential

#### Step 3: Install the App

1. On the GitHub App page, click **"Install App"** in the left sidebar
2. Select your organization (**AdaptiveBS**)
3. Choose repository access:
   - Select: **"Only select repositories"**
   - Choose: **CIApp**
4. Click **"Install"**
5. **Note down the Installation ID** from the URL:
   - URL will be: `https://github.com/organizations/AdaptiveBS/settings/installations/12345678`
   - Installation ID is the number at the end: `12345678`

#### Step 4: Configure Your Application

**For Local Development:**

Edit `DeploymentAPI/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ASPNETCORE_ENVIRONMENT": "Development",
    "StorageType": "InMemory",
    "GitHub:AuthType": "GitHubApp",
    "GitHub:AppId": "123456",
    "GitHub:InstallationId": "12345678",
    "GitHub:PrivateKey": "-----BEGIN RSA PRIVATE KEY-----\nMIIEpAIBAAKCAQEA...\n-----END RSA PRIVATE KEY-----",
    "GitHub:Owner": "AdaptiveBS",
    "GitHub:Repository": "CIApp"
  }
}
```

**Note:** The private key should be the **entire contents** of the `.pem` file, including the header and footer.

**For Azure (Production):**

1. Go to Azure Portal ? Your Function App
2. Navigate to **Configuration** ? **Application settings**
3. Add the following settings:

| Name | Value |
|------|-------|
| `GitHub:AuthType` | `GitHubApp` |
| `GitHub:AppId` | Your App ID (e.g., `123456`) |
| `GitHub:InstallationId` | Your Installation ID (e.g., `12345678`) |
| `GitHub:PrivateKey` | Entire `.pem` file contents |
| `GitHub:Owner` | `AdaptiveBS` |
| `GitHub:Repository` | `CIApp` |

4. Click **Save** and restart the Function App

#### Step 5: Use Azure Key Vault (Recommended)

For better security in production, store the private key in Azure Key Vault:

1. **Upload private key to Key Vault:**
   ```powershell
   az keyvault secret set --vault-name "your-keyvault" --name "GitHubAppPrivateKey" --file path/to/private-key.pem
   ```

2. **Grant Function App access to Key Vault:**
   - Enable Managed Identity on your Function App
   - Grant it "Get" and "List" secret permissions

3. **Update Application Setting:**
   ```
   GitHub:PrivateKey = @Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/GitHubAppPrivateKey/)
   ```

## Testing the Setup

### Test with PowerShell Script

```powershell
# Start your Functions
./start-functions.ps1

# Test GitHub integration
./test-github-integration.ps1
```

You should see in the logs:
```
?? Using GitHub App authentication
? GitHub integration configured
```

### Verify Authentication Type

Make a request to any GitHub endpoint and check the logs:

```powershell
curl http://localhost:7071/api/github/repository
```

Check the Function App logs - you should see:
```
Using GitHub App authentication (App ID: 123456, Installation ID: 12345678)
Generating new GitHub App installation token
Successfully obtained GitHub App installation token (expires at ...)
```

## Configuration Comparison

### Personal Access Token Configuration

```json
{
  "GitHub:AuthType": "PAT",
  "GitHub:Token": "ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "GitHub:Owner": "AdaptiveBS",
  "GitHub:Repository": "CIApp"
}
```

### GitHub App Configuration

```json
{
  "GitHub:AuthType": "GitHubApp",
  "GitHub:AppId": "123456",
  "GitHub:InstallationId": "12345678",
  "GitHub:PrivateKey": "-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----",
  "GitHub:Owner": "AdaptiveBS",
  "GitHub:Repository": "CIApp"
}
```

## Token Lifecycle

### Personal Access Token
- ? Simple to create and use
- ?? Long-lived (no expiration or 90 days)
- ?? Manual rotation required
- ?? Revoked if user leaves organization

### GitHub App Token
- ? Auto-generated from private key
- ? Short-lived (1 hour)
- ? Automatically rotated
- ? Cached in-memory for performance
- ? Survives user changes

## Security Best Practices

1. **Never commit credentials to git**
   - Private keys are in `.gitignore`
   - `local.settings.json` is excluded

2. **Use Azure Key Vault in production**
   - Store private key as a secret
   - Use Managed Identity to access

3. **Rotate credentials regularly**
   - PAT: Every 90 days
   - GitHub App: Generate new private key yearly

4. **Monitor usage**
   - Check GitHub App installations page
   - Review audit logs
   - Monitor rate limits

5. **Principle of least privilege**
   - Only grant required permissions
   - Use read-only when possible

## Troubleshooting

### Error: "GitHub:AppId is not configured"
**Solution:** Set `GitHub:AuthType` to `GitHubApp` and provide all required settings

### Error: "Invalid GitHub App private key format"
**Solution:** Ensure you copied the entire `.pem` file including `-----BEGIN` and `-----END` lines

### Error: "Failed to obtain GitHub App installation token"
**Solution:** 
- Verify App ID and Installation ID are correct
- Check that the app is installed on the repository
- Ensure private key is valid

### Rate Limit Exceeded
**PAT:** Wait for reset (check headers: `X-RateLimit-Reset`)
**GitHub App:** Should have 15k/hour - check if you're actually using GitHub App auth

## Migration Path

**Recommended Approach:**

1. **Development:** Start with Personal Access Token (quick and easy)
2. **Testing:** Continue with PAT or set up GitHub App
3. **Production:** Switch to GitHub App for better security and rate limits

**To Switch:**

Just change `GitHub:AuthType` in configuration:
```json
// From PAT
"GitHub:AuthType": "PAT"

// To GitHub App
"GitHub:AuthType": "GitHubApp"
```

The code automatically detects and uses the appropriate authentication method!

## API Rate Limits

### Check Your Current Rate Limit

```bash
curl -H "Authorization: Bearer YOUR_TOKEN" https://api.github.com/rate_limit
```

### Response Example

```json
{
  "resources": {
    "core": {
      "limit": 15000,        // GitHub App: 15000, PAT: 5000
      "remaining": 14999,
      "reset": 1234567890
    }
  }
}
```

## Summary

| Scenario | Recommended Method |
|----------|-------------------|
| ?? Development | Personal Access Token |
| ?? Testing | Personal Access Token |
| ?? Production | GitHub App |
| ?? Organization-wide | GitHub App |
| ?? Personal projects | Personal Access Token |
| ?? High API usage | GitHub App (3x rate limit) |

**Your implementation now supports BOTH methods** - choose based on your needs! ??
