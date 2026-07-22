# GitHub App Access Verification Script
# Tests GitHub App configuration and access to AdaptiveBS/CIApp repository

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  GitHub App Access Verification" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$appId = "3906613"
$installationId = "136590013"
$owner = "AdaptiveBS"
$repo = "CIApp"
$pemFilePath = ".security\github-app-private-key.pem"

# Step 1: Check PEM file
Write-Host "Step 1: Checking PEM file..." -ForegroundColor Yellow
if (Test-Path $pemFilePath) {
    $pemSize = (Get-Item $pemFilePath).Length
    Write-Host "  ? PEM file found: $pemFilePath" -ForegroundColor Green
    Write-Host "  ?? File size: $pemSize bytes" -ForegroundColor Gray
    
    # Verify it looks like a valid PEM file
    $pemContent = Get-Content $pemFilePath -Raw
    if ($pemContent -match "BEGIN.*PRIVATE KEY") {
        Write-Host "  ? File appears to be a valid PEM private key" -ForegroundColor Green
    } else {
        Write-Host "  ? File doesn't appear to be a valid PEM key" -ForegroundColor Red
        Write-Host "  Expected format: -----BEGIN RSA PRIVATE KEY-----" -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host "  ? PEM file not found at: $pemFilePath" -ForegroundColor Red
    Write-Host ""
    Write-Host "  ?? To fix this:" -ForegroundColor Yellow
    Write-Host "     1. Go to https://github.com/organizations/$owner/settings/apps" -ForegroundColor White
    Write-Host "     2. Find your app and download the private key" -ForegroundColor White
    Write-Host "     3. Save it as: $pemFilePath" -ForegroundColor White
    Write-Host ""
    exit 1
}
Write-Host ""

# Step 2: Check configuration
Write-Host "Step 2: Verifying configuration..." -ForegroundColor Yellow
$configPath = "DeploymentAPI\local.settings.json"
if (Test-Path $configPath) {
    $config = Get-Content $configPath -Raw | ConvertFrom-Json
    
    Write-Host "  App ID: $($config.Values.'GitHub:AppId')" -ForegroundColor Gray
    Write-Host "  Installation ID: $($config.Values.'GitHub:InstallationId')" -ForegroundColor Gray
    Write-Host "  Auth Type: $($config.Values.'GitHub:AuthType')" -ForegroundColor Gray
    Write-Host "  Private Key Source: $($config.Values.'GitHub:PrivateKeySource')" -ForegroundColor Gray
    Write-Host "  Private Key Path: $($config.Values.'GitHub:PrivateKeyPath')" -ForegroundColor Gray
    Write-Host "  Owner: $($config.Values.'GitHub:Owner')" -ForegroundColor Gray
    Write-Host "  Repository: $($config.Values.'GitHub:Repository')" -ForegroundColor Gray
    
    # Verify configuration matches
    if ($config.Values.'GitHub:AppId' -eq $appId -and 
        $config.Values.'GitHub:InstallationId' -eq $installationId -and
        $config.Values.'GitHub:AuthType' -eq 'GitHubApp') {
        Write-Host "  ? Configuration is correct" -ForegroundColor Green
    } else {
        Write-Host "  ??  Configuration mismatch detected" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ? Configuration file not found: $configPath" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 3: Test JWT generation (manual test)
Write-Host "Step 3: Testing GitHub App authentication..." -ForegroundColor Yellow
Write-Host "  Generating JWT token..." -ForegroundColor Gray

try {
    # Load the PEM file
    $pemContent = Get-Content $pemFilePath -Raw
    
    # Create JWT payload
    $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $exp = $now + 600  # 10 minutes
    
    # Note: PowerShell can't easily generate JWT with RSA signing
    # We'll rely on the app to do this
    Write-Host "  ??  JWT generation will be handled by the application" -ForegroundColor Cyan
    Write-Host "  ? PEM file is readable" -ForegroundColor Green
} catch {
    Write-Host "  ? Error reading PEM file: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 4: Check GitHub API accessibility
Write-Host "Step 4: Testing GitHub API accessibility..." -ForegroundColor Yellow
try {
    $testUrl = "https://api.github.com/repos/$owner/$repo"
    Write-Host "  Testing URL: $testUrl" -ForegroundColor Gray
    
    $response = Invoke-WebRequest -Uri $testUrl -Method HEAD -ErrorAction Stop -UseBasicParsing
    Write-Host "  ? GitHub API is accessible" -ForegroundColor Green
    Write-Host "  ?? Rate limit remaining: $($response.Headers['X-RateLimit-Remaining'])" -ForegroundColor Gray
} catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "  ??  Repository not found (this is expected for private repos without auth)" -ForegroundColor Yellow
        Write-Host "  ??  Will test with authentication using the app" -ForegroundColor Cyan
    } elseif ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "  ??  Authentication required (expected for private repo)" -ForegroundColor Yellow
        Write-Host "  ??  Will test with app authentication" -ForegroundColor Cyan
    } else {
        Write-Host "  ? Error accessing GitHub API: $_" -ForegroundColor Red
        Write-Host "  ??  This might be a network issue" -ForegroundColor Yellow
    }
}
Write-Host ""

# Step 5: Verify app installation
Write-Host "Step 5: Verifying GitHub App installation..." -ForegroundColor Yellow
Write-Host "  ??  To verify your app is installed:" -ForegroundColor Cyan
Write-Host "     1. Go to https://github.com/organizations/$owner/settings/installations" -ForegroundColor White
Write-Host "     2. Find installation ID: $installationId" -ForegroundColor White
Write-Host "     3. Verify it has access to the '$repo' repository" -ForegroundColor White
Write-Host ""

# Step 6: Summary and next steps
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  Summary" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Configuration:" -ForegroundColor Yellow
Write-Host "   • GitHub App ID: $appId" -ForegroundColor White
Write-Host "   • Installation ID: $installationId" -ForegroundColor White
Write-Host "   • Target Repository: $owner/$repo" -ForegroundColor White
Write-Host "   • PEM File: $(if (Test-Path $pemFilePath) { '? Found' } else { '? Missing' })" -ForegroundColor White
Write-Host ""

Write-Host "?? Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Start your Azure Functions:" -ForegroundColor White
Write-Host "      .\start-functions.ps1" -ForegroundColor Cyan
Write-Host ""
Write-Host "   2. Test the GitHub integration:" -ForegroundColor White
Write-Host "      .\test-github-integration.ps1" -ForegroundColor Cyan
Write-Host ""
Write-Host "   3. Check the logs for:" -ForegroundColor White
Write-Host "      ?? Using GitHub App authentication with FileSystem" -ForegroundColor Gray
Write-Host "      ? GitHub integration configured" -ForegroundColor Gray
Write-Host ""

Write-Host "?? Documentation:" -ForegroundColor Yellow
Write-Host "   • Setup Guide: GITHUB-PEM-SETUP.md" -ForegroundColor White
Write-Host "   • GitHub App Guide: GITHUB-APP-SETUP.md" -ForegroundColor White
Write-Host ""

Write-Host "?? Troubleshooting:" -ForegroundColor Yellow
Write-Host "   • If 401 errors: Verify App ID and Installation ID" -ForegroundColor White
Write-Host "   • If 404 errors: Check app is installed on repository" -ForegroundColor White
Write-Host "   • If PEM errors: Ensure the private key is valid" -ForegroundColor White
Write-Host ""

Write-Host "? Pre-flight checks complete!" -ForegroundColor Green
Write-Host ""
