# GitHub App PEM File Setup Guide

## ?? Using PEM File for Local Development

### Step 1: Create Secrets Directory

In your project root, create a `secrets` folder:

```powershell
# From project root (where DeploymentAPI folder is)
New-Item -ItemType Directory -Path "DeploymentAPI\secrets" -Force
```

**Note:** This folder is already in `.gitignore`, so your PEM file will never be committed!

### Step 2: Save Your PEM File

1. Download your GitHub App private key from GitHub (it will be named something like `your-app-name.2024-01-15.private-key.pem`)
2. Copy it to the secrets folder:

```powershell
Copy-Item "path\to\downloads\your-app.2024-01-15.private-key.pem" `
          "DeploymentAPI\secrets\github-app-private-key.pem"
```

Or simply drag and drop the file into `DeploymentAPI\secrets\` and rename it to `github-app-private-key.pem`

### Step 3: Configure `local.settings.json`

Update `DeploymentAPI/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ASPNETCORE_ENVIRONMENT": "Development",
    "StorageType": "InMemory",
    
    "GitHub:AuthType": "GitHubApp",
    "GitHub:PrivateKeySource": "File",
    "GitHub:PrivateKeyPath": "secrets/github-app-private-key.pem",
    "GitHub:AppId": "123456",
    "GitHub:InstallationId": "12345678",
    "GitHub:Owner": "AdaptiveBS",
    "GitHub:Repository": "CIApp"
  }
}
```

**Important Configuration Values:**
- `GitHub:PrivateKeySource` = `"File"` (reads from disk)
- `GitHub:PrivateKeyPath` = Path relative to `DeploymentAPI` folder

### Step 4: Verify Setup

Your folder structure should look like:

```
DeplomentStatus/
??? DeploymentAPI/
?   ??? secrets/
?   ?   ??? github-app-private-key.pem  ? Your PEM file here
?   ??? Functions/
?   ??? Services/
?   ??? local.settings.json
?   ??? ...
??? ...
```

### Step 5: Test

```powershell
./start-functions.ps1
./test-github-integration.ps1
```

You should see in the logs:
```
?? Using file system for GitHub App private key
?? Using GitHub App authentication
? GitHub integration configured
Successfully loaded private key from file
```

---

## ?? Using Azure Key Vault (Production)

### Step 1: Upload PEM to Key Vault

```powershell
# Login to Azure
az login

# Upload the private key to Key Vault
az keyvault secret set `
  --vault-name "your-keyvault-name" `
  --name "GitHubAppPrivateKey" `
  --file "path\to\your-private-key.pem"
```

### Step 2: Enable Managed Identity

In Azure Portal:
1. Go to your Function App
2. Navigate to **Identity** ? **System assigned**
3. Turn status **On**
4. Click **Save**

### Step 3: Grant Key Vault Access

```powershell
# Get the Function App's principal ID
$principalId = az functionapp identity show `
  --name "your-function-app-name" `
  --resource-group "your-resource-group" `
  --query principalId -o tsv

# Grant access to Key Vault
az keyvault set-policy `
  --name "your-keyvault-name" `
  --object-id $principalId `
  --secret-permissions get list
```

### Step 4: Configure Application Settings

In Azure Portal ? Function App ? Configuration ? Application settings:

```
GitHub:AuthType = GitHubApp
GitHub:PrivateKeySource = KeyVault
GitHub:KeyVaultUrl = https://your-keyvault.vault.azure.net
GitHub:KeyVaultSecretName = GitHubAppPrivateKey
GitHub:AppId = 123456
GitHub:InstallationId = 12345678
GitHub:Owner = AdaptiveBS
GitHub:Repository = CIApp
```

### Step 5: Restart Function App

After saving configuration, restart your Function App.

---

## ?? Configuration Options Summary

### Option 1: File System (Local Development) ? Recommended

**Use when:** Local testing, development

```json
{
  "GitHub:AuthType": "GitHubApp",
  "GitHub:PrivateKeySource": "File",
  "GitHub:PrivateKeyPath": "secrets/github-app-private-key.pem",
  "GitHub:AppId": "123456",
  "GitHub:InstallationId": "12345678"
}
```

**Pros:**
- ? Easy to set up
- ? No Azure dependencies for local dev
- ? File is in .gitignore (safe)

**Cons:**
- ?? Not suitable for production
- ?? PEM file must be deployed with app

---

### Option 2: Azure Key Vault (Production) ? Recommended

**Use when:** Production, staging, shared environments

```json
{
  "GitHub:AuthType": "GitHubApp",
  "GitHub:PrivateKeySource": "KeyVault",
  "GitHub:KeyVaultUrl": "https://your-vault.vault.azure.net",
  "GitHub:KeyVaultSecretName": "GitHubAppPrivateKey",
  "GitHub:AppId": "123456",
  "GitHub:InstallationId": "12345678"
}
```

**Pros:**
- ? Most secure
- ? Centralized secret management
- ? Access control and auditing
- ? No secrets in code or config

**Cons:**
- ?? Requires Azure Key Vault setup
- ?? Requires Managed Identity

---

### Option 3: Configuration/Environment Variable (Alternative)

**Use when:** Quick testing, CI/CD pipelines

```json
{
  "GitHub:AuthType": "GitHubApp",
  "GitHub:PrivateKeySource": "Configuration",
  "GitHub:PrivateKey": "-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----",
  "GitHub:AppId": "123456",
  "GitHub:InstallationId": "12345678"
}
```

**Pros:**
- ? Simple configuration
- ? Works in any environment

**Cons:**
- ?? Private key in configuration
- ?? Less secure than Key Vault

---

## ?? Security Best Practices

### For Local Development:
1. ? Store PEM in `secrets/` folder (already in .gitignore)
2. ? Never commit PEM files
3. ? Don't share PEM files via email/Slack
4. ? Each developer can have their own test GitHub App

### For Production:
1. ? Always use Azure Key Vault
2. ? Use Managed Identity (no credentials needed)
3. ? Rotate private keys annually
4. ? Monitor Key Vault access logs
5. ? Use separate GitHub Apps for dev/staging/prod

---

## ?? Testing Different Configurations

You can easily switch between configurations:

```powershell
# Test with PEM file
# (Edit local.settings.json: PrivateKeySource = "File")
./start-functions.ps1

# Test with Key Vault (requires Azure setup)
# (Edit local.settings.json: PrivateKeySource = "KeyVault")
./start-functions.ps1
```

---

## ?? Troubleshooting

### Error: "GitHub App private key file not found"

**Solution:** Check that:
- File exists at `DeploymentAPI/secrets/github-app-private-key.pem`
- Path in config is correct (relative to DeploymentAPI folder)
- File has the correct extension (.pem)

### Error: "Failed to parse GitHub App private key"

**Solution:** Ensure PEM file contains:
- `-----BEGIN RSA PRIVATE KEY-----` header
- Base64 encoded key data
- `-----END RSA PRIVATE KEY-----` footer

### Error: "Failed to retrieve from Azure Key Vault"

**Solution:**
- Verify Key Vault URL is correct
- Check Managed Identity is enabled
- Confirm access policy is configured
- Check secret name matches configuration

### Logs show: "Using configuration for GitHub App private key"

This means `PrivateKeySource` is not set correctly. Set it to:
- `"File"` for file system
- `"KeyVault"` for Azure Key Vault

---

## ?? Quick Setup Checklist

### Local Development:
- [ ] Create `DeploymentAPI/secrets/` folder
- [ ] Copy PEM file to `secrets/github-app-private-key.pem`
- [ ] Update `local.settings.json` with `PrivateKeySource = "File"`
- [ ] Add `GitHub:PrivateKeyPath` pointing to PEM file
- [ ] Add `GitHub:AppId` and `GitHub:InstallationId`
- [ ] Test with `./start-functions.ps1`

### Azure Production:
- [ ] Upload PEM to Azure Key Vault
- [ ] Enable Managed Identity on Function App
- [ ] Grant Key Vault access to Function App
- [ ] Add Application Settings with `PrivateKeySource = "KeyVault"`
- [ ] Add `GitHub:KeyVaultUrl` and other settings
- [ ] Restart Function App
- [ ] Test endpoints

---

## ?? Pro Tips

1. **Use absolute path for testing:**
   ```json
   "GitHub:PrivateKeyPath": "C:\\full\\path\\to\\your-key.pem"
   ```

2. **Test with both auth methods:** Keep PAT config as backup
   ```json
   // Switch between:
   "GitHub:AuthType": "PAT"  // Simple
   "GitHub:AuthType": "GitHubApp"  // Production-ready
   ```

3. **Monitor Key Vault costs:** Key Vault operations are charged (but very cheap)

4. **Rotate keys regularly:** Generate new PEM yearly and update Key Vault

---

Your PEM file setup is now complete! ??

Start with **File System** for local development, then upgrade to **Azure Key Vault** for production.
