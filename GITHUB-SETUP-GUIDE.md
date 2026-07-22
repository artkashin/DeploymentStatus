# GitHub Integration Setup Guide

## Overview

This guide will help you connect your Deployment Status application to the private GitHub repository `AdaptiveBS/CIApp` to fetch deployment actions and workflow information.

## Prerequisites

- Access to the `AdaptiveBS/CIApp` private repository
- GitHub account with permissions to create Personal Access Tokens
- Azure Functions running locally or in Azure

## Step-by-Step Setup

### 1. Create a GitHub Personal Access Token (PAT)

1. Go to GitHub and log in with your account
2. Navigate to **Settings** ? **Developer settings** ? **Personal access tokens** ? **Tokens (classic)**
   - Direct link: https://github.com/settings/tokens
3. Click **Generate new token** ? **Generate new token (classic)**
4. Configure the token:
   - **Note**: Enter a descriptive name (e.g., "DeploymentAPI - CIApp Access")
   - **Expiration**: Choose an appropriate expiration period (recommend 90 days or 1 year)
   - **Select scopes**: Check the following:
     - ? `repo` - Full control of private repositories
       - This includes: repo:status, repo_deployment, public_repo, repo:invite, security_events
     - ? `workflow` - Update GitHub Action workflows (optional, for triggering workflows)
5. Click **Generate token** at the bottom
6. **?? IMPORTANT**: Copy the token immediately! You won't be able to see it again.
   - The token will look like: `ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

### 2. Configure Local Development

Update your `DeploymentAPI/local.settings.json` file:

```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "ASPNETCORE_ENVIRONMENT": "Development",
        "StorageType": "InMemory",
        "GitHub:Token": "ghp_your_actual_token_here",
        "GitHub:Owner": "AdaptiveBS",
        "GitHub:Repository": "CIApp"
    }
}
```

If using Table Storage, also update `DeploymentAPI/local.settings.tablestorage.json`:

```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "ASPNETCORE_ENVIRONMENT": "Development",
        "StorageType": "TableStorage",
        "GitHub:Token": "ghp_your_actual_token_here",
        "GitHub:Owner": "AdaptiveBS",
        "GitHub:Repository": "CIApp"
    }
}
```

### 3. Configure Azure (Production)

For production deployment in Azure:

1. Go to your Azure Function App in the Azure Portal
2. Navigate to **Configuration** ? **Application settings**
3. Add the following new application settings:

| Name | Value | Description |
|------|-------|-------------|
| `GitHub:Token` | `ghp_your_token_here` | Your GitHub Personal Access Token |
| `GitHub:Owner` | `AdaptiveBS` | GitHub organization/user name |
| `GitHub:Repository` | `CIApp` | Repository name |

4. Click **Save** and restart the Function App

#### Using Azure Key Vault (Recommended for Production)

For better security, store the GitHub token in Azure Key Vault:

1. Create an Azure Key Vault (if you don't have one)
2. Add a secret named `GitHubToken` with your PAT value
3. Grant your Function App's Managed Identity access to the Key Vault
4. Update the Application Setting:
   ```
   GitHub:Token = @Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/GitHubToken/)
   ```

### 4. Test the Integration

Run the test script to verify the integration:

```powershell
# Make sure your Functions are running first
./start-functions.ps1

# In a new terminal, run the test
./test-github-integration.ps1
```

The test script will:
- ? Fetch repository information
- ? List all workflows
- ? Get recent workflow runs
- ? Test filtering by client name

### 5. Verify Access

You can manually verify your token works:

```bash
# Replace YOUR_TOKEN with your actual token
curl -H "Authorization: Bearer YOUR_TOKEN" \
     -H "Accept: application/vnd.github+json" \
     https://api.github.com/repos/AdaptiveBS/CIApp
```

If successful, you should see repository information.

## Using the API

### Available Endpoints

Once configured, you can use these endpoints:

1. **Get all workflow runs**:
   ```bash
   curl http://localhost:7071/api/github/actions
   ```

2. **Get workflow runs for a specific client**:
   ```bash
   curl "http://localhost:7071/api/github/actions?client=ClientName"
   ```

3. **Get all workflows**:
   ```bash
   curl http://localhost:7071/api/github/workflows
   ```

4. **Get repository information**:
   ```bash
   curl http://localhost:7071/api/github/repository
   ```

## Workflow Run Data Structure

Each workflow run includes:

- `id` - Unique run ID
- `name` - Workflow run name
- `status` - Current status (queued, in_progress, completed)
- `conclusion` - Result (success, failure, cancelled, skipped)
- `headBranch` - Branch that triggered the run
- `createdAt` - When the run was created
- `updatedAt` - Last update time
- `htmlUrl` - Link to view the run on GitHub
- `actor` - User who triggered the run

## Troubleshooting

### Error: 401 Unauthorized

**Problem**: GitHub API returns 401 Unauthorized

**Solutions**:
1. Verify your token is correctly set in `local.settings.json`
2. Check that the token hasn't expired
3. Ensure the token has the `repo` scope
4. Confirm you have access to the `AdaptiveBS/CIApp` repository

### Error: 404 Not Found

**Problem**: Repository not found

**Solutions**:
1. Verify `GitHub:Owner` is set to `AdaptiveBS`
2. Verify `GitHub:Repository` is set to `CIApp`
3. Ensure your GitHub account has access to the repository
4. Check if you're a member of the AdaptiveBS organization

### Error: 403 Forbidden

**Problem**: Rate limit exceeded or insufficient permissions

**Solutions**:
1. Check GitHub API rate limits: https://api.github.com/rate_limit
2. Wait for the rate limit to reset
3. Verify your token has the correct scopes
4. Consider implementing caching to reduce API calls

### Functions not starting

**Problem**: Azure Functions won't start

**Solutions**:
1. Run `./diagnose.ps1` to check for issues
2. Verify all dependencies are installed
3. Check that `local.settings.json` exists and is valid JSON
4. Ensure .NET 8 SDK is installed

## Security Best Practices

1. **Never commit tokens to git**
   - `local.settings.json` is already in `.gitignore`
   - Always use environment variables or Key Vault in production

2. **Rotate tokens regularly**
   - Set an expiration date on your tokens
   - Create a reminder to rotate before expiration

3. **Use minimal permissions**
   - Only grant the `repo` scope (don't use admin scopes unless needed)
   - Consider using fine-grained tokens with read-only access if possible

4. **Monitor token usage**
   - Check GitHub's audit log for unusual activity
   - Revoke tokens immediately if compromised

5. **Use Azure Key Vault for production**
   - Store secrets in Key Vault, not in Application Settings
   - Use Managed Identity to access Key Vault

## Next Steps

- [ ] Set up automatic syncing of workflow runs to your deployment database
- [ ] Create webhooks for real-time updates
- [ ] Add filtering and search capabilities
- [ ] Implement caching to reduce API calls
- [ ] Create a dashboard view for GitHub Actions

## Additional Resources

- **[GITHUB-APP-SETUP.md](GITHUB-APP-SETUP.md)** - Complete GitHub App setup guide (recommended for production)
- **[GITHUB-INTEGRATION.md](DeploymentAPI/GITHUB-INTEGRATION.md)** - Technical API documentation
- **[GITHUB-QUICK-REFERENCE.md](GITHUB-QUICK-REFERENCE.md)** - Quick reference card
- [GitHub REST API Documentation](https://docs.github.com/en/rest)
- [GitHub Actions API](https://docs.github.com/en/rest/actions)
- [Creating Personal Access Tokens](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/app-service/app-service-key-vault-references)
